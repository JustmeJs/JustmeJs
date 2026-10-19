using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace KleinesSpiel.Scripts.Rooms
{
    public partial class DungeonManager : Node
    {
        public static DungeonManager Instance { get; private set; }
        [Export] public int SeedOverride;
        private Node _roomContainer;
        private Player.Player _player;
        private DungeonFloor _floor;
        private Room _current;
        private readonly Dictionary<string, Room> _instances = new();
        private readonly Dictionary<string, MinimapRoomInfo> _map = new();
        private readonly Queue<int> _items = new();
        private int _seed;
        private bool _transitionQueued;
        private int _arrivalFrames;
        private bool _finished;
        private double _runSeconds;
        public double GetRunSeconds() => _runSeconds;
        private PackedScene _pauseMenuScene;
        private UI.PauseMenu _pauseMenu;
        public event Action MapChanged;
        public int GetZoneIndex() => _floor?.Zone ?? 0;
        public int GetRunSeed() => _seed;
        public string GetZoneName() => _floor?.Name ?? "";
        public string GetTheme() => _floor?.Theme ?? "crypt";
        public int GetFloorCount() => DungeonFloor.FloorCount;
        public string GetCurrentRoomId() => _current?.Definition.Id ?? "";
        public Room GetCurrentRoom() => _current;
        public bool GetPortalAvailable() => _floor?.PortalOutcome == true;
        public bool GetRunFinished() => _finished;
        public IReadOnlyDictionary<string, MinimapRoomInfo> GetMapEntries() => _map;

        public override void _Ready()
        {
            Instance = this;
            ProcessMode = ProcessModeEnum.Always;
            _pauseMenuScene = GD.Load<PackedScene>("res://Scenes/UI/PauseMenu.tscn");
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!GetTree().Paused && _arrivalFrames > 0) _arrivalFrames--;
            if (!GetTree().Paused && !_finished && IsInstanceValid(_player) && _player.IsInsideTree() && _player.GetCurrentHealthHalfHearts() > 0) _runSeconds += delta;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!@event.IsActionPressed("pause")) return;
            if (IsInstanceValid(_pauseMenu))
            {
                _pauseMenu.QueueFree();
                _pauseMenu = null;
                GetTree().Paused = false;
            }
            else if (!GetTree().Paused && IsInstanceValid(_player) && !_finished)
            {
                _pauseMenu = _pauseMenuScene.Instantiate<UI.PauseMenu>();
                _pauseMenu.Closed += () => _pauseMenu = null;
                GetTree().Root.AddChild(_pauseMenu);
                GetTree().Paused = true;
            }
            GetViewport().SetInputAsHandled();
        }

        public void Initialize(Node roomContainer, Player.Player player)
        {
            ReleaseRooms();
            _roomContainer = roomContainer;
            _player = player;
            _seed = SeedOverride != 0 ? SeedOverride : Random.Shared.Next(1, int.MaxValue);
            Audio.GameAudio.Instance?.StartRun(_seed);
            var bag = Enumerable.Range(0, Items.ItemCatalog.Paths.Length).ToList();
            DungeonFloor.Shuffle(bag, new Random(_seed ^ 0x195af));
            _items.Clear();
            foreach (int index in bag) _items.Enqueue(index);
            _finished = false;
            _runSeconds = 0;
            _transitionQueued = false;
            BeginZone(1);
        }

        private void BeginZone(int zone)
        {
            ReleaseRooms();
            ClearProjectiles();
            _floor = DungeonFloor.Generate(_seed, zone);
            foreach (var room in _floor.Rooms.Values.Where(r => r.Kind == RoomMapType.Treasure))
                room.ItemIndex = _items.Dequeue();
            _map.Clear();
            foreach (var shop in _floor.Rooms.Values.Where(r => r.Kind == RoomMapType.Shop)) Discover(shop.Id, false);
            EnterRoom(_floor.StartId, new Vector2(512, 288));
            Hud()?.ShowItemPopup(Common.Localization.Format("Floor {0}/6: {1}", "Ebene {0}/6: {1}", zone, _floor.Name),
                Common.Localization.Text("Defeat the boss to advance.", "Besiege den Boss, um weiterzukommen."), 3);
        }

        private void ReleaseRooms()
        {
            foreach (var room in _instances.Values)
                if (IsInstanceValid(room)) room.QueueFree();
            _instances.Clear();
            _current = null;
        }

        public override void _ExitTree()
        {
            ReleaseRooms();
            if (Instance == this) Instance = null;
        }

        public void ForgetRun()
        {
            // Detached rooms do not belong to Main; release them before retry/menu.
            ReleaseRooms();
            _map.Clear();
        }

        public bool Travel(int direction)
        {
            if (_transitionQueued || _arrivalFrames > 0 || _finished || GetTree().Paused || !IsInstanceValid(_current)) return false;
            var dir = (Door.Direction)direction;
            if (!_current.Definition.Neighbors.ContainsKey(dir) || !_current.IsCleared()) return false;
            _transitionQueued = true;
            CallDeferred(nameof(FinishTravel), direction);
            return true;
        }

        private void FinishTravel(int direction)
        {
            if (!IsInstanceValid(_current) || _finished || GetTree().Paused
                || _player.GetCurrentHealthHalfHearts() <= 0) { _transitionQueued = false; return; }
            var dir = (Door.Direction)direction;
            string target = _current.Definition.Neighbors[dir];
            Vector2 arrival = dir switch
            {
                Door.Direction.Up => new Vector2(512, 480), Door.Direction.Down => new Vector2(512, 96),
                Door.Direction.Left => new Vector2(704, 288), _ => new Vector2(320, 288)
            };
            EnterRoom(target, arrival);
            _transitionQueued = false;
        }

        private void EnterRoom(string id, Vector2 spawn)
        {
            if (IsInstanceValid(_current) && _current.IsInsideTree()) _roomContainer.RemoveChild(_current);
            ClearProjectiles();
            _player.GlobalPosition = spawn;
            _player.Velocity = Vector2.Zero;
            if (!_instances.TryGetValue(id, out var room))
            {
                room = new Room { Name = "DungeonRoom", Definition = _floor.Rooms[id], Floor = _floor };
                _instances.Add(id, room);
            }
            _current = room;
            // Cached Area2D nodes need a physics sync after the player's teleport.
            // Ignore stale overlap signals from the room's previous activation.
            _arrivalFrames = 3;
            _roomContainer.AddChild(room);
            room.OnPlayerEntered();
            _player.OnEnteredNewRoom();
            Hud()?.HideBossHealthBar();
            room.RefreshBossHud();
            Audio.GameAudio.Instance?.EnterRoom(_floor, room);
            Discover(id, true);
            foreach (var neighbor in room.Definition.Neighbors.Values) Discover(neighbor, false);
            MapChanged?.Invoke();
        }

        private void Discover(string id, bool visited)
        {
            if (!_map.TryGetValue(id, out var entry))
            {
                var data = _floor.Rooms[id];
                entry = new MinimapRoomInfo { GridPosition = data.Position, Type = data.Kind };
                _map.Add(id, entry);
            }
            entry.Visited |= visited;
        }

        public void OnRoomCleared(Room room)
        {
            if (room != _current) return;
            MapChanged?.Invoke();
            Audio.GameAudio.Instance?.EnterRoom(_floor, room);
            if (room.Definition.Kind != RoomMapType.Boss || _floor.PortalOutcome.HasValue || _finished) return;
            _floor.PortalOutcome = !_floor.IsFinal;
            if (_floor.PortalOutcome == true)
            {
                room.CreatePortal();
                Hud()?.ShowItemPopup(Common.Localization.Text("The way forward is open!", "Der Weg ist frei!"),
                    Common.Localization.Format("Enter the portal to {0}.", "Betritt das Portal zu {0}.", DungeonFloor.NameFor(_floor.Zone + 1)), 5);
            }
            else FinishRun();
        }

        public bool RequestNextZone(string roomId)
        {
            if (_transitionQueued || _arrivalFrames > 0 || _finished || GetTree().Paused || _floor.IsFinal
                || _floor.PortalOutcome != true || GetCurrentRoomId() != roomId
                || !_current.IsCleared() || roomId != _floor.BossId) return false;
            _transitionQueued = true;
            CallDeferred(nameof(FinishNextZone));
            return true;
        }

        private void FinishNextZone()
        {
            if (GetTree().Paused || _player.GetCurrentHealthHalfHearts() <= 0) { _transitionQueued = false; return; }
            BeginZone(_floor.Zone + 1);
            _transitionQueued = false;
        }

        private void FinishRun()
        {
            _finished = true;
            Audio.GameAudio.Instance?.OnRunEnded();
            GetTree().Paused = true;
            var screen = GD.Load<PackedScene>("res://Scenes/UI/VictoryScreen.tscn").Instantiate();
            GetTree().Root.AddChild(screen);
        }

        private UI.HUD Hud() => GetTree().GetFirstNodeInGroup("hud") as UI.HUD;
        private void ClearProjectiles()
        {
            foreach (var projectile in GetTree().GetNodesInGroup("projectiles"))
            {
                projectile.ProcessMode = ProcessModeEnum.Disabled;
                if (projectile is Area2D area) { area.CollisionLayer = 0; area.CollisionMask = 0; }
                projectile.QueueFree();
            }
        }

        // Read-only previews also let tests inspect hundreds of seeds without spawning enemies.
        public Godot.Collections.Array<Godot.Collections.Dictionary> PreviewFloor(int seed, int zone)
            => Snapshot(DungeonFloor.Generate(seed, zone));
        public Godot.Collections.Array<Godot.Collections.Dictionary> GetFloorSnapshot() => Snapshot(_floor);
        public Godot.Collections.Array<Godot.Collections.Dictionary> GetMapSnapshot()
        {
            var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            foreach (var pair in _map)
                result.Add(new Godot.Collections.Dictionary { ["id"] = pair.Key,
                    ["visited"] = pair.Value.Visited, ["position"] = pair.Value.GridPosition });
            return result;
        }
        private static Godot.Collections.Array<Godot.Collections.Dictionary> Snapshot(DungeonFloor floor)
        {
            var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            if (floor == null) return result;
            foreach (var room in floor.Rooms.Values)
            {
                var neighbors = new Godot.Collections.Dictionary();
                foreach (var pair in room.Neighbors) neighbors[(int)pair.Key] = pair.Value;
                result.Add(new Godot.Collections.Dictionary
                {
                    ["id"] = room.Id, ["position"] = room.Position, ["kind"] = room.Kind.ToString(),
                    ["layout"] = room.Layout, ["neighbors"] = neighbors, ["cleared"] = room.Cleared,
                    ["collected"] = room.TreasureCollected, ["item"] = room.ItemIndex,
                    ["shop_item"] = room.ShopItemIndex, ["shop_sold"] = room.ShopPassiveSold,
                    ["boss_scene"] = floor.BossScene, ["theme"] = floor.Theme, ["floor"] = floor.Zone
                });
            }
            return result;
        }
    }
}
