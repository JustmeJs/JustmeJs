using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KleinesSpiel.Scripts.Rooms
{
    public sealed class RoomDefinition
    {
        public string Id;
        public Vector2I Position;
        public RoomMapType Kind;
        public int Layout;
        public int ContentSeed;
        public int ItemIndex = -1;
        public bool Cleared;
        public bool TreasureCollected;
        public int ShopItemIndex;
        public bool ShopPassiveSold;
        public readonly bool[] ShopHeartsSold = new bool[2];
        public readonly Dictionary<Door.Direction, string> Neighbors = new();
    }

    // Pure run data: room identity is independent of any reused scene/template.
    public sealed class DungeonFloor
    {
        public const int RoomCount = 12;
        public const int FloorCount = 6;
        public readonly Dictionary<string, RoomDefinition> Rooms = new();
        public int Zone;
        public string StartId;
        public string BossId;
        public string BossScene;
        public bool? PortalOutcome;
        public bool IsCastle => Zone >= 4;
        public bool IsFinal => Zone == FloorCount;
        public string Theme => IsCastle ? "castle" : "crypt";
        public string Name => NameFor(Zone);
        public static string NameFor(int floor) => Common.Localization.Format(
            floor <= 3 ? "Ash Crypt {0}" : "Castle {0}",
            floor <= 3 ? "Aschekrypta {0}" : "Schloss {0}", (floor - 1) % 3 + 1);
        public static readonly Vector2I[] Offsets = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };

        public static DungeonFloor Generate(int seed, int zone)
        {
            if (zone < 1 || zone > FloorCount) throw new ArgumentOutOfRangeException(nameof(zone));
            for (int attempt = 0; attempt < 128; attempt++)
            {
                var random = new Random(unchecked(seed + zone * 104729 + attempt * 7919));
                var cells = new HashSet<Vector2I> { Vector2I.Zero,
                    new(random.Next(2) == 0 ? -1 : 1, 0), new(0, random.Next(2) == 0 ? -1 : 1) };
                while (cells.Count < RoomCount)
                {
                    var frontier = new List<Vector2I>();
                    for (int x = -2; x <= 2; x++)
                    for (int y = -2; y <= 2; y++)
                    {
                        var at = new Vector2I(x, y);
                        if (!cells.Contains(at) && Offsets.Count(d => cells.Contains(at + d)) == 1)
                            frontier.Add(at);
                    }
                    if (frontier.Count == 0) break;
                    cells.Add(frontier[random.Next(frontier.Count)]);
                }
                if (cells.Count != RoomCount || cells.Min(p => p.X) >= 0 || cells.Max(p => p.X) <= 0
                    || cells.Min(p => p.Y) >= 0 || cells.Max(p => p.Y) <= 0) continue;
                var leaves = cells.Where(p => p != Vector2I.Zero && Offsets.Count(d => cells.Contains(p + d)) == 1).ToList();
                if (leaves.Count < 3) continue;
                var distances = new Dictionary<Vector2I, int> { [Vector2I.Zero] = 0 };
                var queue = new Queue<Vector2I>();
                queue.Enqueue(Vector2I.Zero);
                while (queue.Count > 0)
                {
                    var at = queue.Dequeue();
                    foreach (var offset in Offsets)
                        if (cells.Contains(at + offset) && !distances.ContainsKey(at + offset))
                        {
                            distances[at + offset] = distances[at] + 1;
                            queue.Enqueue(at + offset);
                        }
                }
                int farthest = leaves.Max(p => distances[p]);
                var farLeaves = leaves.Where(p => distances[p] == farthest).ToList();
                var boss = farLeaves[random.Next(farLeaves.Count)];
                leaves.Remove(boss);
                Shuffle(leaves, random);
                var treasures = leaves.Take(1).ToHashSet();
                var shop = leaves[1];
                var layouts = Enumerable.Range(0, 6).ToList();
                Shuffle(layouts, random);
                var floor = new DungeonFloor { Zone = zone, StartId = Id(zone, Vector2I.Zero), BossId = Id(zone, boss) };
                var bossPool = zone switch
                {
                    1 or 2 => new[] { "res://Scenes/Enemies/BossMelee.tscn", "res://Scenes/Enemies/BossRanged.tscn" },
                    3 => new[] { "res://Scenes/Enemies/Boss.tscn" },
                    4 => new[] { "res://Scenes/Enemies/BossRanged.tscn", "res://Scenes/Enemies/BossFourWay.tscn" },
                    5 => new[] { "res://Scenes/Enemies/Boss.tscn", "res://Scenes/Enemies/BossFourWay.tscn" },
                    _ => new[] { "res://Scenes/Enemies/CastleBoss.tscn" }
                };
                floor.BossScene = bossPool[random.Next(bossPool.Length)];
                int normalIndex = 0;
                foreach (var position in cells.OrderBy(p => p.Y).ThenBy(p => p.X))
                {
                    var kind = position == Vector2I.Zero ? RoomMapType.Start : position == boss ? RoomMapType.Boss
                        : treasures.Contains(position) ? RoomMapType.Treasure : position == shop ? RoomMapType.Shop : RoomMapType.Normal;
                    var definition = new RoomDefinition
                    {
                        Id = Id(zone, position), Position = position, Kind = kind,
                        Layout = kind == RoomMapType.Normal ? layouts[normalIndex++ % layouts.Count] : 0,
                        ContentSeed = random.Next(),
                        ShopItemIndex = Items.Economy.ShopItem(random),
                    };
                    for (int d = 0; d < 4; d++)
                        if (cells.Contains(position + Offsets[d])) definition.Neighbors[(Door.Direction)d] = Id(zone, position + Offsets[d]);
                    floor.Rooms.Add(definition.Id, definition);
                }
                return floor;
            }
            throw new InvalidOperationException($"Could not generate connected dungeon for seed {seed}, zone {zone}.");
        }

        private static string Id(int zone, Vector2I position) => $"z{zone}:{position.X},{position.Y}";
        public static void Shuffle<T>(IList<T> values, Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
