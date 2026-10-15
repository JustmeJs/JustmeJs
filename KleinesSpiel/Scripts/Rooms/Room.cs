using System;
using System.Linq;
using Godot;

namespace KleinesSpiel.Scripts.Rooms
{
    public partial class Room : Node2D
    {
        public RoomDefinition Definition;
        public DungeonFloor Floor;
        private Node2D _enemies;
        private int _alive;
        private Enemies.Enemy _boss;
        private bool _portalQueued;
        public bool IsCleared() => Definition.Cleared;
        public string GetRoomId() => Definition.Id;
        public string GetRoomKind() => Definition.Kind.ToString();
        public int GetLayoutIndex() => Definition.Layout;
        public int GetAliveCount() => _alive;

        public override void _Ready()
        {
            BuildGeometry();
            BuildDoors();
            _enemies = new Node2D { Name = "Enemies" };
            AddChild(_enemies);
            SpawnContent();
            Definition.Cleared = _alive == 0;
            SetDoors(Definition.Cleared);
        }

        private void BuildGeometry()
        {
            AddChild(new TextureRect
            {
                Name = "Floor", Position = new Vector2(272, 48), Size = new Vector2(480, 480),
                Texture = GD.Load<Texture2D>("res://Assets/Sprites/Tiles/stone_floor.svg"),
                StretchMode = TextureRect.StretchModeEnum.Tile, MouseFilter = Control.MouseFilterEnum.Ignore,
                ZIndex = -20, SelfModulate = Floor.Zone == 2 ? new Color(1.02f, 0.72f, 1.15f) : Colors.White
            });
            var walls = new StaticBody2D { Name = "Walls", CollisionLayer = 1, CollisionMask = 0 };
            AddChild(walls);
            for (int d = 0; d < 4; d++)
            {
                bool horizontal = d < 2;
                Vector2 center = DoorPosition((Door.Direction)d);
                Vector2 axis = horizontal ? Vector2.Right : Vector2.Down;
                if (Definition.Neighbors.ContainsKey((Door.Direction)d))
                {
                    AddRectangle(walls, center - axis * 136, horizontal ? new Vector2(208, 32) : new Vector2(32, 208));
                    AddRectangle(walls, center + axis * 136, horizontal ? new Vector2(208, 32) : new Vector2(32, 208));
                }
                else AddRectangle(walls, center, horizontal ? new Vector2(480, 32) : new Vector2(32, 480));
            }
            var obstacles = new StaticBody2D { Name = "Obstacles", CollisionLayer = 1, CollisionMask = 0 };
            AddChild(obstacles);
            foreach (var rectangle in RoomLayouts.Obstacles(Definition.Layout))
                AddRectangle(obstacles, rectangle.GetCenter(), rectangle.Size);
            AddChild(new RoomArt { Name = "RoomArt", ZIndex = -10,
                StoneTint = Floor.Zone == 2 ? new Color(1.1f, 0.75f, 1.2f) : Colors.White,
                Amethyst = Floor.Zone == 2, RoomKind = Definition.Kind });
            if (Definition.Kind == RoomMapType.Start)
            {
                AddChild(new Label
                {
                    Name = "TutorialLabel", Position = new Vector2(305, 225), Size = new Vector2(414, 110),
                    Text = $"{Floor.Name}\nWASD: Bewegen   Pfeile: Zaubern\nLeertaste: Dash   Esc: Pause\nErkunde den Dungeon in alle Richtungen!",
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore
                });
            }
        }

        public static void AddRectangle(Node parent, Vector2 position, Vector2 size, float rotation = 0)
        {
            parent.AddChild(new CollisionShape2D
            { Name = "CollisionShape2D", Position = position, Rotation = rotation, Shape = new RectangleShape2D { Size = size } });
        }

        public static Vector2 DoorPosition(Door.Direction direction) => direction switch
        {
            Door.Direction.Up => new Vector2(512, 48), Door.Direction.Down => new Vector2(512, 528),
            Door.Direction.Left => new Vector2(272, 288), _ => new Vector2(752, 288)
        };

        private void BuildDoors()
        {
            var doors = new Node2D { Name = "Doors" };
            AddChild(doors);
            foreach (var pair in Definition.Neighbors)
            {
                var door = new Door { Name = $"Door{pair.Key}", Position = DoorPosition(pair.Key),
                    TargetRoomId = pair.Value, SourceRoomId = Definition.Id, DoorDirection = pair.Key,
                    TargetKind = Floor.Rooms[pair.Value].Kind };
                var blocker = new StaticBody2D { Name = "Blocker", CollisionLayer = 1, CollisionMask = 0 };
                door.AddChild(blocker);
                float rotation = pair.Key is Door.Direction.Left or Door.Direction.Right ? Mathf.Pi / 2 : 0;
                AddRectangle(blocker, Vector2.Zero, new Vector2(64, 32), rotation);
                var trigger = new Area2D { Name = "TriggerArea", CollisionLayer = 0, CollisionMask = 2 };
                AddRectangle(trigger, Vector2.Zero, new Vector2(64, 32), rotation);
                door.AddChild(trigger);
                door.AddChild(new Sprite2D { Name = "DoorSprite", Rotation = rotation,
                    Scale = new Vector2(64f / 48, 1),
                    Texture = GD.Load<Texture2D>("res://Assets/Sprites/Tiles/door_closed.svg") });
                doors.AddChild(door);
            }
        }

        private void SpawnContent()
        {
            if (Definition.Kind == RoomMapType.Treasure && !Definition.TreasureCollected)
            {
                var pickup = GD.Load<PackedScene>("res://Scenes/Items/ItemPickup.tscn").Instantiate<Items.ItemPickup>();
                pickup.Name = "TreasureItem";
                pickup.Data = Items.ItemCatalog.Get(Definition.ItemIndex);
                pickup.Position = new Vector2(512, 288);
                pickup.Collected += () => Definition.TreasureCollected = true;
                AddChild(pickup);
            }
            if (Definition.Cleared) return;
            var random = new Random(Definition.ContentSeed);
            if (Definition.Kind == RoomMapType.Boss)
            {
                _boss = GD.Load<PackedScene>(Floor.BossScene).Instantiate<Enemies.Enemy>();
                if (Floor.Zone == 2) _boss.MaxHealth *= 1.5f;
                AddEnemy(_boss, new Vector2(512, 200));
                RefreshBossHud();
            }
            else if (Definition.Kind == RoomMapType.Normal)
            {
                var points = new[] { new Vector2(352,144), new Vector2(512,144), new Vector2(672,144),
                    new Vector2(352,288), new Vector2(672,288), new Vector2(352,432), new Vector2(512,432), new Vector2(672,432) };
                var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
                var safe = points.Where(p => !RoomLayouts.Obstacles(Definition.Layout).Any(r => r.Grow(22).HasPoint(p))
                    && (player == null || player.Position.DistanceTo(p) > 90)).ToList();
                DungeonFloor.Shuffle(safe, random);
                string[] types = { "res://Scenes/Enemies/EnemyMelee.tscn", "res://Scenes/Enemies/EnemyRanged.tscn", "res://Scenes/Enemies/EnemyFourWay.tscn" };
                int count = Math.Min(safe.Count, random.Next(2, Floor.Zone == 1 ? 4 : 5));
                for (int i = 0; i < count; i++)
                {
                    var enemy = GD.Load<PackedScene>(types[random.Next(types.Length)]).Instantiate<Enemies.Enemy>();
                    if (Floor.Zone == 2) { enemy.MaxHealth *= 1.4f; enemy.MoveSpeed *= 1.1f; }
                    AddEnemy(enemy, safe[i]);
                }
            }
        }

        private void AddEnemy(Enemies.Enemy enemy, Vector2 position)
        {
            enemy.Position = position;
            enemy.Defeated += OnDefeated;
            _alive++;
            _enemies.AddChild(enemy);
        }

        private void OnDefeated(Enemies.Enemy enemy)
        {
            _alive--;
            if (_alive != 0 || Definition.Cleared) return;
            Definition.Cleared = true;
            SetDoors(true);
            DungeonManager.Instance?.OnRoomCleared(this);
        }

        private void SetDoors(bool open)
        {
            foreach (var child in GetNode("Doors").GetChildren())
                if (child is Door door) { if (open) door.SetOpen(); else door.SetClosed(); }
        }

        public void RefreshBossHud()
        {
            if (IsInstanceValid(_boss) && _boss.GetCurrentHealth() > 0)
                (GetTree().GetFirstNodeInGroup("hud") as UI.HUD)?.ShowBossHealthBar(_boss,
                    Floor.Zone == 2 ? "Herrscher der Amethyst-Ruinen" : "Wächter der Aschekrypta");
        }

        public void CreatePortal()
        {
            if (_portalQueued || HasNode("ZonePortal")) return;
            _portalQueued = true;
            CallDeferred(nameof(SpawnPortal));
        }

        private void SpawnPortal()
        {
            if (!HasNode("ZonePortal"))
                AddChild(new ZonePortal { Name = "ZonePortal", SourceRoomId = Definition.Id, Position = new Vector2(512, 360) });
            _portalQueued = false;
        }
    }
}
