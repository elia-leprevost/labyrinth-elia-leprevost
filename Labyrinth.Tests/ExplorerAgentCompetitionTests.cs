using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Labyrinth.Crawl;
using Labyrinth.Exploration;
using Labyrinth.Items;
using Labyrinth.Tiles;
using Xunit;

namespace Labyrinth.Tests;

public class ExplorerAgentCompetitionTests
{
    [Fact]
    public async Task Locked_door_is_not_recorded_as_wall_when_walk_fails()
    {
        await using var coord = new MapCoordinator();
        coord.Start();

        var bag = new MyInventory();
        var crawler = new FakeCrawler(
            startX: 0,
            startY: 0,
            startDir: Direction.East,
            facingSequence: new[]
            {
                typeof(Door), 
                typeof(Door), 
            },
            tryWalkResults: new Inventory?[]
            {
                null, 
            }
        );

        var agent = new ExplorerAgent("A1", crawler, bag, coord);

        await agent.RunAsync(TimeSpan.FromMilliseconds(80), CancellationToken.None);
        await Task.Delay(50); 

        var snap = coord.Snapshot();
        snap[new Position(1, 0)].Should().Be(CellKind.Door);  
    }

    [Fact]
    public async Task Agent_picks_up_keys_from_room_inventory_after_successful_walk()
    {
        await using var coord = new MapCoordinator();
        coord.Start();

        var bag = new MyInventory();

        var roomInv = new LocalInventory(new Key());

        var crawler = new FakeCrawler(
            startX: 0,
            startY: 0,
            startDir: Direction.East,
            facingSequence: new[]
            {
                typeof(Room), 
                typeof(Wall)  
            },
            tryWalkResults: new Inventory?[]
            {
                roomInv 
            },
            onSuccessfulWalkMoveBy: (1, 0)
        );

        var agent = new ExplorerAgent("A1", crawler, bag, coord);

        await agent.RunAsync(TimeSpan.FromMilliseconds(120), CancellationToken.None);

        bag.HasItems.Should().BeTrue();
        bag.Items.Should().ContainSingle(i => i is Key);

        roomInv.HasItems.Should().BeFalse();
    }
    private sealed class FakeCrawler : ICrawler
    {
        private readonly Queue<Type> _facing;
        private readonly Queue<Inventory?> _walkResults;
        private readonly (int dx, int dy)? _moveBy;

        public FakeCrawler(
            int startX,
            int startY,
            Direction startDir,
            IEnumerable<Type> facingSequence,
            IEnumerable<Inventory?> tryWalkResults,
            (int dx, int dy)? onSuccessfulWalkMoveBy = null)
        {
            X = startX;
            Y = startY;
            Direction = (Direction)startDir.Clone();

            _facing = new Queue<Type>(facingSequence);
            _walkResults = new Queue<Inventory?>(tryWalkResults);
            _moveBy = onSuccessfulWalkMoveBy;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public Direction Direction { get; }

        public Task<Type> FacingTileType
        {
            get
            {
                if (_facing.Count == 0) return Task.FromResult(typeof(Wall));
                return Task.FromResult(_facing.Peek());
            }
        }

        public Task<Inventory?> TryWalk(Inventory myInventory)
        {
            Inventory? result = _walkResults.Count > 0 ? _walkResults.Dequeue() : null;

            if (result is not null)
            {
                if (_facing.Count > 0) _facing.Dequeue();

                if (_moveBy is { } d)
                {
                    X += d.dx;
                    Y += d.dy;
                }
                else
                {
                    X += Direction.DeltaX;
                    Y += Direction.DeltaY;
                }
            }
            return Task.FromResult(result);
        }
    }
}
