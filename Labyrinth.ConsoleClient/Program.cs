using System.Text.Json;
using Labyrinth;
using Labyrinth.ApiClient;
using Labyrinth.Build;
using Labyrinth.Exploration;
using Labyrinth.Items;

using Dto = ApiTypes;

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  Local:   Labyrinth.ConsoleClient");
    Console.WriteLine("  Remote:  Labyrinth.ConsoleClient <serverUrl> <appKeyGuid> [settings.json] [agents=3] [seconds=60]");
}

ContestSession? contest = null;
Labyrinth.Labyrinth labyrinth;
var crawlers = new List<(Labyrinth.Crawl.ICrawler crawler, Inventory bag, string id)>();

if (args.Length == 0)
{
    labyrinth = new Labyrinth.Labyrinth(new AsciiParser("""
+--+--------+
|  /        |
|  +--+--+  |
|     |k    |
+--+  |  +--+
   |k  x    |
+  +-------/|
|           |
+-----------+
"""));
    for (var i = 0; i < 3; i++)
    {
        var c = labyrinth.NewCrawler();
        var bag = new LocalInventory(); // per crawler
        crawlers.Add((c, bag, $"local-{i+1}"));
    }
}
else
{
    if (args.Length < 2)
    {
        PrintUsage();
        return;
    }

    var serverUrl = new Uri(args[0]);
    var appKey = Guid.Parse(args[1]);

    Dto.Settings? settings = null;
    if (args.Length >= 3 && File.Exists(args[2]))
        settings = JsonSerializer.Deserialize<Dto.Settings>(File.ReadAllText(args[2]));

    contest = await ContestSession.Open(serverUrl, appKey, settings);
    labyrinth = new Labyrinth.Labyrinth(contest.Builder);

    var agentCount = args.Length >= 4 && int.TryParse(args[3], out var n) ? Math.Clamp(n, 1, 3) : 3;
    for (var i = 0; i < agentCount; i++)
    {
        var c = await contest.NewCrawler();
        var bag = contest.Bags.ElementAt(i);
        crawlers.Add((c, bag, $"remote-{i+1}"));
    }
}

var seconds = args.Length >= 5 && int.TryParse(args[4], out var s) ? Math.Max(5, s) : 60;

Console.WriteLine("Labyrinth starting... (Ctrl+C to stop)");
Console.WriteLine(labyrinth.ToString());

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

await using var coordinator = new MapCoordinator();
coordinator.Start();

var tasks = new List<Task>();
foreach (var (crawler, bag, id) in crawlers)
{
    var agent = new ExplorerAgent(id, crawler, bag, coordinator);
    tasks.Add(agent.RunAsync(TimeSpan.FromSeconds(seconds), cts.Token));
}

var reporter = Task.Run(async () =>
{
    while (!cts.IsCancellationRequested)
    {
        await Task.Delay(1000, cts.Token).ContinueWith(_ => { });
        var snap = coordinator.Snapshot();
        var known = snap.Count;
        var frontiers = coordinator.Map.GetFrontiers().Distinct().Count();
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] known={known} frontiers={frontiers}");
    }
}, cts.Token);

tasks.Add(reporter);

try
{
    await Task.WhenAll(tasks);
}
catch (OperationCanceledException)
{
    // expected
}
finally
{
    if (contest is not null)
        await contest.Close();
}

Console.WriteLine("Done.");
