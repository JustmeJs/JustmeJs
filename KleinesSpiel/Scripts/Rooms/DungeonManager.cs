using System.Collections.Generic;
using Godot;

namespace KleinesSpiel.Scripts.Rooms
{
	// Singleton (als Autoload registriert), das weiss, welcher Raum gerade
	// aktiv ist, und den Wechsel zwischen Raeumen uebernimmt.
	public partial class DungeonManager : Node
	{
		public static DungeonManager Instance { get; private set; }

		private Node _roomContainer;
		private Node2D _currentRoomInstance;
		private Player.Player _player;

		// Merkt sich, welche Raeume (per Szenen-Pfad) in diesem Durchlauf
		// schon geleert wurden, damit ihre Gegner nicht erneut spawnen,
		// wenn man zurueckgeht und wieder hineingeht.
		private readonly HashSet<string> _clearedRoomPaths = new();

		// Merkt sich, in welchen TreasureRooms das Item in diesem Durchlauf
		// schon abgeholt wurde - verhindert, dass beim erneuten Betreten ein
		// weiteres (zweites) Item spawnt.
		private readonly HashSet<string> _collectedTreasurePaths = new();

		// Gitterposition + Typ jeder bekannten Sala, fuer die Minimap.
		// Wird automatisch aus den Door.DoorDirection-Werten aufgebaut -
		// das eigentliche Kartenlayout (welche Tuer zu welcher Sala fuehrt)
		// bleibt komplett in den Room-Szenen definiert wie bisher.
		private readonly Dictionary<string, MinimapRoomInfo> _mapEntries = new();
		private string _currentRoomScenePath;

		// Feuert jedes Mal, wenn sich die Minimap-Daten aendern (neue Sala
		// betreten / neue Nachbar-Sala entdeckt), damit die Minimap-UI nur
		// bei Bedarf neu zeichnet statt jeden Frame zu pollen.
		public event System.Action MapChanged;

		private PackedScene _pauseMenuScene;
		private UI.PauseMenu _activePauseMenu;

		public override void _Ready()
		{
			Instance = this;

			// WICHTIG: Always nur hier auf dem Autoload setzen, NICHT auf
			// MainController/Main - sonst "erbt" der Player (und alles
			// andere unter Main) ueber den Standard-Modus "Inherit" dieses
			// Always und bewegt sich munter weiter, obwohl pausiert ist.
			// DungeonManager liegt als Autoload ausserhalb von Main.tscn,
			// ist also kein Vorfahre des Players - sein Always bleibt lokal.
			ProcessMode = ProcessModeEnum.Always;

			_pauseMenuScene = GD.Load<PackedScene>("res://Scenes/UI/PauseMenu.tscn");
		}

		public override void _UnhandledInput(InputEvent @event)
		{
			if (@event.IsActionPressed("pause"))
			{
				TogglePauseMenu();
				GetViewport().SetInputAsHandled();
			}
		}

		private void TogglePauseMenu()
		{
			if (IsInstanceValid(_activePauseMenu))
			{
				_activePauseMenu.QueueFree();
				_activePauseMenu = null;
				GetTree().Paused = false;
				return;
			}

			// Spiel ist schon aus einem anderen Grund pausiert (Game Over /
			// Sieg-Bildschirm) -> hier kein zusaetzliches Pause-Menu oeffnen.
			if (GetTree().Paused)
				return;

			// Kein aktiver Spielstand (z.B. man steht noch im Hauptmenue) ->
			// Escape soll dort nichts tun.
			if (!IsInstanceValid(_player))
				return;

			var instance = _pauseMenuScene.Instantiate<UI.PauseMenu>();
			instance.Closed += () => _activePauseMenu = null;
			GetTree().Root.AddChild(instance);
			_activePauseMenu = instance;
			GetTree().Paused = true;
		}

		// Wird einmal von Main.cs beim Start aufgerufen (auch bei jedem Neustart,
		// da Main.tscn dann komplett neu geladen wird).
		public void Initialize(Node roomContainer, Player.Player player)
		{
			_roomContainer = roomContainer;
			_player = player;

			// Wichtig: Die alte Referenz stammt aus der vorherigen Szene und wurde
			// beim Szenenwechsel bereits zerstoert. Ohne dieses Zuruecksetzen
			// wuerde ChangeRoom() versuchen, ein bereits geloeschtes Objekt
			// erneut freizugeben, was den Neustart (Retry) verhindert hat.
			_currentRoomInstance = null;

			// Neuer Durchlauf -> kein Raum ist mehr "bereits geleert".
			_clearedRoomPaths.Clear();
			_collectedTreasurePaths.Clear();
			_mapEntries.Clear();
			_currentRoomScenePath = null;
		}

		public bool IsRoomCleared(string scenePath)
		{
			return !string.IsNullOrEmpty(scenePath) && _clearedRoomPaths.Contains(scenePath);
		}

		public void MarkRoomCleared(string scenePath)
		{
			if (!string.IsNullOrEmpty(scenePath))
			{
				_clearedRoomPaths.Add(scenePath);
			}
		}

		public bool IsTreasureCollected(string scenePath)
		{
			return !string.IsNullOrEmpty(scenePath) && _collectedTreasurePaths.Contains(scenePath);
		}

		public void MarkTreasureCollected(string scenePath)
		{
			if (!string.IsNullOrEmpty(scenePath))
			{
				_collectedTreasurePaths.Add(scenePath);
			}
		}

		public void ChangeRoom(string scenePath, string spawnPointName, Door.Direction? arrivalDirection = null)
		{
			if (_roomContainer == null || _player == null)
			{
				GD.PrintErr("DungeonManager wurde nicht initialisiert.");
				return;
			}

			if (_currentRoomInstance != null && IsInstanceValid(_currentRoomInstance))
			{
				_currentRoomInstance.QueueFree();
			}
			_currentRoomInstance = null;

			var packedScene = GD.Load<PackedScene>(scenePath);
			if (packedScene == null)
			{
				GD.PrintErr($"Raum-Szene nicht gefunden: {scenePath}");
				return;
			}

			var instance = packedScene.Instantiate<Node2D>();
			_roomContainer.AddChild(instance);
			_currentRoomInstance = instance;

			if (!string.IsNullOrEmpty(spawnPointName))
			{
				var spawnMarker = instance.GetNodeOrNull<Marker2D>(spawnPointName);
				if (spawnMarker != null)
				{
					_player.GlobalPosition = spawnMarker.GlobalPosition;
				}
			}

			RegisterVisitedRoom(scenePath, arrivalDirection);
			_currentRoomScenePath = scenePath;

			if (instance is Room room)
			{
				room.OnPlayerEntered(_player);
				RegisterNeighbourRooms(room, scenePath);
			}

			MapChanged?.Invoke();
		}

		// ---------------------------------------------------------------
		// Minimap: Gitterposition + Typ jeder Sala, automatisch aus den
		// Door.DoorDirection-Werten abgeleitet.
		// ---------------------------------------------------------------

		public System.Collections.Generic.IReadOnlyDictionary<string, MinimapRoomInfo> GetMapEntries() => _mapEntries;
		public string GetCurrentRoomScenePath() => _currentRoomScenePath;

		private void RegisterVisitedRoom(string scenePath, Door.Direction? arrivalDirection)
		{
			if (!_mapEntries.TryGetValue(scenePath, out var entry))
			{
				Vector2I gridPosition = Vector2I.Zero;

				if (arrivalDirection.HasValue
					&& !string.IsNullOrEmpty(_currentRoomScenePath)
					&& _mapEntries.TryGetValue(_currentRoomScenePath, out var fromEntry))
				{
					gridPosition = fromEntry.GridPosition + DirectionOffset(arrivalDirection.Value);
				}

				entry = new MinimapRoomInfo
				{
					GridPosition = gridPosition,
					Type = InferRoomType(scenePath),
				};
				_mapEntries[scenePath] = entry;
			}

			entry.Visited = true;
		}

		// Sobald man eine Sala betritt, "sieht" man auch die Tueren zu ihren
		// direkten Nachbarn - die werden als unbesuchte (graue) Eintraege
		// aufgenommen, falls sie noch nicht bekannt sind.
		private void RegisterNeighbourRooms(Room room, string roomScenePath)
		{
			if (!_mapEntries.TryGetValue(roomScenePath, out var selfEntry))
				return;

			var doorsContainer = room.GetNodeOrNull<Node>("Doors");
			if (doorsContainer == null)
				return;

			foreach (Node child in doorsContainer.GetChildren())
			{
				if (child is Door door && !string.IsNullOrEmpty(door.TargetRoomScenePath) && !_mapEntries.ContainsKey(door.TargetRoomScenePath))
				{
					_mapEntries[door.TargetRoomScenePath] = new MinimapRoomInfo
					{
						GridPosition = selfEntry.GridPosition + DirectionOffset(door.DoorDirection),
						Type = InferRoomType(door.TargetRoomScenePath),
						Visited = false,
					};
				}
			}
		}

		private static Vector2I DirectionOffset(Door.Direction direction)
		{
			return direction switch
			{
				Door.Direction.Up => new Vector2I(0, -1),
				Door.Direction.Down => new Vector2I(0, 1),
				Door.Direction.Left => new Vector2I(-1, 0),
				Door.Direction.Right => new Vector2I(1, 0),
				_ => Vector2I.Zero,
			};
		}

		private static RoomMapType InferRoomType(string scenePath)
		{
			if (scenePath.Contains("StartRoom"))
				return RoomMapType.Start;
			if (scenePath.Contains("TreasureRoom"))
				return RoomMapType.Treasure;
			if (scenePath.Contains("BossRoom"))
				return RoomMapType.Boss;
			return RoomMapType.Normal;
		}
	}
}
