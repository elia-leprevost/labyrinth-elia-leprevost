using ApiTypes;
using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Labyrinth Training Server", Version = "v1" });
});

builder.Services.AddSingleton<MazeSession>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapPost("/crawlers", async (Guid appKey, Settings? settings, MazeSession session) =>
{
    var crawler = session.CreateCrawler();
    return Results.Ok(crawler);
});

app.MapPatch("/crawlers/{id:guid}", async (Guid id, Guid appKey, Crawler update, MazeSession session) =>
{
    var res = session.UpdateCrawler(id, update);
    return res is null ? Results.NotFound() : Results.Ok(res);
});

app.MapDelete("/crawlers/{id:guid}", (Guid id, Guid appKey, MazeSession session) =>
{
    return session.DeleteCrawler(id) ? Results.Ok() : Results.NotFound();
});

app.MapPut("/crawlers/{id:guid}/{fromType}", async (Guid id, string fromType, Guid appKey, InventoryItem[] moveRequests, MazeSession session) =>
{
    var res = session.MoveItems(id, fromType, moveRequests);
    return res is null ? Results.NotFound() : Results.Ok(res);
});

app.Run();

sealed class MazeSession
{
    private readonly object _gate = new();
    private readonly Tile[,] _tiles;
    private readonly (int x, int y) _start;

    private readonly Dictionary<Guid, CrawlerState> _crawlers = new();

    public MazeSession()
    {
        var parser = new AsciiParser("""
+--+--------+
|  /        |
|  +--+--+  |
|     |k    |
+--+  |  +--+
   |k  x    |
+  +-------/|
|           |
+-----------+
""");
        (int x, int y) start = (-1, -1);
        parser.StartPositionFound += (_, e) => start = (e.X, e.Y);
        _tiles = parser.Build();
        _start = start;
    }

    public Crawler CreateCrawler()
    {
        lock (_gate)
        {
            if (_crawlers.Count >= 3)
                throw new InvalidOperationException("Max 3 crawlers per session.");

            var id = Guid.NewGuid();
            var state = new CrawlerState
            {
                Id = id,
                X = _start.x,
                Y = _start.y,
                Dir = ApiTypes.Direction.North,
                Bag = new LocalInventory(),
            };

            _crawlers[id] = state;
            return ToDto(state);
        }
    }

    public bool DeleteCrawler(Guid id)
    {
        lock (_gate) return _crawlers.Remove(id);
    }

    public Crawler? UpdateCrawler(Guid id, Crawler update)
    {
        lock (_gate)
        {
            if (!_crawlers.TryGetValue(id, out var st)) return null;

            st.Dir = update.Dir;
            st.Walking = update.Walking;

            if (st.Walking)
            {
                st.Walking = false;
                var (dx, dy) = ToDelta(st.Dir);
                var fx = st.X + dx;
                var fy = st.Y + dy;

                var tile = GetTileOrOutside(fx, fy);
                if (tile is Door door && door.IsLocked)
                {
                    try { _ = door.Open(st.Bag); } catch { /* ignore */ }
                }

                if (tile.IsTraversable)
                {
                    st.X = fx;
                    st.Y = fy;
                }
            }

            return ToDto(st);
        }
    }

    public InventoryItem[]? MoveItems(Guid id, string fromType, InventoryItem[] moveRequests)
    {
        lock (_gate)
        {
            if (!_crawlers.TryGetValue(id, out var st)) return null;

            var (from, to) = fromType.ToLowerInvariant() switch
            {
                "bag" => (st.Bag, GetCellInventory(st.X, st.Y)),
                "items" => (GetCellInventory(st.X, st.Y), st.Bag),
                _ => throw new ArgumentException("Inventory type must be 'bag' or 'items'.", nameof(fromType))
            };

            var required = moveRequests.Select(i => i.MoveRequired ?? false).ToList();
            if (required.Count != from.ItemTypes.Count()) return Array.Empty<InventoryItem>();

            _ = to.TryMoveItemsFrom(from, required).Result;

            return from.ItemTypes.Select(_ => new InventoryItem { Type = ItemType.Key }).ToArray();
        }
    }

    private Tile GetTileOrOutside(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _tiles.GetLength(0) || y >= _tiles.GetLength(1))
            return Outside.Singleton;
        return _tiles[x, y];
    }

    private LocalInventory GetCellInventory(int x, int y)
    {
        var tile = GetTileOrOutside(x, y);
        if (!tile.IsTraversable) return new LocalInventory(); 
        return tile.Pass();
    }

    private static (int dx, int dy) ToDelta(ApiTypes.Direction dir) => dir switch
    {
        ApiTypes.Direction.North => (0, -1),
        ApiTypes.Direction.East => (1, 0),
        ApiTypes.Direction.South => (0, 1),
        ApiTypes.Direction.West => (-1, 0),
        _ => (0, -1)
    };

    private Crawler ToDto(CrawlerState st)
    {
        var (dx, dy) = ToDelta(st.Dir);
        var facing = GetTileOrOutside(st.X + dx, st.Y + dy);

        return new Crawler
        {
            Id = st.Id,
            X = st.X,
            Y = st.Y,
            Dir = st.Dir,
            Walking = false,
            FacingTile = facing switch
            {
                Wall => TileType.Wall,
                Room => TileType.Room,
                Door => TileType.Door,
                Outside => TileType.Wall, 
                _ => TileType.Wall
            },
            Bag = st.Bag.ItemTypes.Select(_ => new InventoryItem { Type = ItemType.Key }).ToArray(),
            Items = GetCellInventory(st.X, st.Y).ItemTypes.Select(_ => new InventoryItem { Type = ItemType.Key }).ToArray(),
        };
    }

    private sealed class CrawlerState
    {
        public Guid Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public ApiTypes.Direction Dir { get; set; }
        public bool Walking { get; set; }
        public LocalInventory Bag { get; set; } = new LocalInventory();
    }
}
