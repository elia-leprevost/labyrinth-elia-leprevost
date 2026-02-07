using Xunit;
using FluentAssertions;
using Labyrinth.Exploration;

namespace Labyrinth.Tests;

public class PathfinderTests
{
    [Fact]
    public void BfsPath_returns_shortest_path_on_grid()
    {
        var map = new Dictionary<Position, CellKind>();
        
        for (var x = 0; x < 3; x++)
        for (var y = 0; y < 3; y++)
            map[new Position(x, y)] = CellKind.Room;

        
        map[new Position(1, 1)] = CellKind.Wall;

        var path = Pathfinder.BfsPath(map, new Position(0,0), new Position(2,2));
        path.Should().NotBeEmpty();
        path.First().Should().Be(new Position(0,0));
        path.Last().Should().Be(new Position(2,2));
        
        path.Count.Should().Be(5);
        path.Should().NotContain(new Position(1,1));
    }
}

public class CoordinatorTests
{
    [Fact]
    public async Task Coordinator_updates_map_from_observations()
    {
        await using var coord = new MapCoordinator();
        coord.Start();

        var from = new Position(0,0);
        await coord.PublishAsync(new Observation(from, (1,0), CellKind.Wall, null), CancellationToken.None);

        await Task.Delay(50);

        var snap = coord.Snapshot();
        snap[from].Should().Be(CellKind.Room);
        snap[new Position(1,0)].Should().Be(CellKind.Wall);
    }

    [Fact]
    public async Task Frontiers_are_reserved_to_reduce_overlap()
    {
        await using var coord = new MapCoordinator();
        coord.Start();

        
        await coord.PublishAsync(new Observation(new Position(0,0), (1,0), CellKind.Room, new Position(1,0)), CancellationToken.None);
        await Task.Delay(50);

        var f1 = coord.ReserveFrontier("a1", new Position(0,0));
        var f2 = coord.ReserveFrontier("a2", new Position(0,0));

        f1.Should().NotBeNull();
        
        if (f2 is not null)
            f2.Should().NotBe(f1);
    }
}
