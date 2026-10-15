using Godot;

namespace KleinesSpiel.Scripts.Rooms
{
	// Wird auf die Wurzel jeder Raum-Szene gelegt.
	// Erwartet zwei Kind-Knoten (falls vorhanden):
	//   "Enemies" -> enthaelt alle Enemy-Instanzen dieses Raumes
	//   "Doors"   -> enthaelt alle Door-Instanzen dieses Raumes
	//
	// Zufaellige Gegner (normale Kampfraeume):
	//   "EnemySpawnPoints" -> Marker2D-Kinder, aus denen zufaellig ein paar
	//   ausgewaehlt werden. An jedem gewaehlten Punkt wird ein zufaelliger
	//   Gegnertyp (Melee/Ranged/FourWay) platziert. Das Kartenlayout selbst
	//   (welche Tuer zu welchem Raum fuehrt) bleibt dabei unveraendert.
	//
	// Zufaelliger Boss (nur BossRoom):
	//   "BossSpawnPoint" -> ein einzelner Marker2D. Wenn UseRandomBoss an ist,
	//   wird eine zufaellige Szene aus BossVariants dort platziert.
	public partial class Room : Node2D
	{
		[Export] public bool ShowVictoryOnClear = false;

		[ExportGroup("Zufaellige Gegner")]
		[Export] public bool UseRandomEnemies = true;
		[Export] public int MinRandomEnemies = 2;
		[Export] public int MaxRandomEnemies = 3;

		[ExportGroup("Zufaelliger Boss")]
		[Export] public bool UseRandomBoss = false;
		[Export] public Godot.Collections.Array<PackedScene> BossVariants = new();
		[Export] public string BossDisplayName = "Boss";

		// Die "normalen" Gegnertypen, aus denen bei zufaelligen Kampfraeumen
		// gewaehlt wird. Bosse und ihre groesseren Varianten spawnen NICHT
		// hier, sondern ueber BossVariants (nur in der BossRoom).
		private static readonly string[] RandomEnemyScenePaths =
		{
			"res://Scenes/Enemies/EnemyMelee.tscn",
			"res://Scenes/Enemies/EnemyRanged.tscn",
			"res://Scenes/Enemies/EnemyFourWay.tscn",
		};

		private int _aliveEnemyCount;

		public override void _Ready()
		{
			bool alreadyCleared = DungeonManager.Instance != null && DungeonManager.Instance.IsRoomCleared(SceneFilePath);

			var enemiesContainer = GetNodeOrNull<Node>("Enemies");

			if (!alreadyCleared && enemiesContainer != null)
			{
				var spawnPointsContainer = GetNodeOrNull<Node>("EnemySpawnPoints");
				if (UseRandomEnemies && spawnPointsContainer != null)
				{
					SpawnRandomEnemies(enemiesContainer, spawnPointsContainer);
				}

				if (UseRandomBoss && BossVariants != null && BossVariants.Count > 0)
				{
					SpawnRandomBoss(enemiesContainer);
				}
			}

			if (enemiesContainer != null)
			{
				foreach (Node child in enemiesContainer.GetChildren())
				{
					if (child is Enemies.Enemy enemy)
					{
						if (alreadyCleared)
						{
							// Dieser Raum wurde in diesem Durchlauf schon geleert:
							// die Gegner sollen nicht erneut spawnen.
							enemy.DisableAndFree();
						}
						else
						{
							_aliveEnemyCount++;
							enemy.Defeated += OnEnemyDefeated;
						}
					}
				}
			}

			if (_aliveEnemyCount > 0)
			{
				SetDoors(open: false);
			}
			else
			{
				SetDoors(open: true);

				if (!alreadyCleared)
				{
					MarkThisRoomCleared();
				}
			}
		}

		// Waehlt zufaellig 2-3 (siehe MinRandomEnemies/MaxRandomEnemies) der
		// vordefinierten Spawn-Punkte aus und stellt dort jeweils einen
		// zufaelligen Gegnertyp hin. Laeuft nur beim ERSTEN Betreten eines
		// Raumes (siehe alreadyCleared oben), damit beim Zurueckgehen keine
		// neuen Gegner nachspawnen.
		private void SpawnRandomEnemies(Node enemiesContainer, Node spawnPointsContainer)
		{
			var spawnPoints = new System.Collections.Generic.List<Marker2D>();
			foreach (Node child in spawnPointsContainer.GetChildren())
			{
				if (child is Marker2D marker)
					spawnPoints.Add(marker);
			}
			if (spawnPoints.Count == 0)
				return;

			var rng = new RandomNumberGenerator();
			rng.Randomize();

			int maxPossible = Mathf.Min(MaxRandomEnemies, spawnPoints.Count);
			int minPossible = Mathf.Min(MinRandomEnemies, maxPossible);
			int count = rng.RandiRange(minPossible, maxPossible);

			// Spawn-Punkte mischen (Fisher-Yates), damit nicht immer dieselben
			// zuerst drankommen.
			for (int i = spawnPoints.Count - 1; i > 0; i--)
			{
				int j = rng.RandiRange(0, i);
				(spawnPoints[i], spawnPoints[j]) = (spawnPoints[j], spawnPoints[i]);
			}

			for (int i = 0; i < count; i++)
			{
				string scenePath = RandomEnemyScenePaths[rng.RandiRange(0, RandomEnemyScenePaths.Length - 1)];
				var scene = GD.Load<PackedScene>(scenePath);
				if (scene == null)
					continue;

				var enemyInstance = scene.Instantiate<Enemies.Enemy>();
				enemiesContainer.AddChild(enemyInstance);
				enemyInstance.GlobalPosition = spawnPoints[i].GlobalPosition;
			}
		}

		// Waehlt eine zufaellige Boss-Variante aus BossVariants und platziert
		// sie an "BossSpawnPoint". Zeigt danach die Boss-Lebensleiste in der HUD.
		private void SpawnRandomBoss(Node enemiesContainer)
		{
			var bossSpawnPoint = GetNodeOrNull<Marker2D>("BossSpawnPoint");
			if (bossSpawnPoint == null)
				return;

			var rng = new RandomNumberGenerator();
			rng.Randomize();

			var chosenScene = BossVariants[rng.RandiRange(0, BossVariants.Count - 1)];
			if (chosenScene == null)
				return;

			var bossInstance = chosenScene.Instantiate<Enemies.Enemy>();
			enemiesContainer.AddChild(bossInstance);
			bossInstance.GlobalPosition = bossSpawnPoint.GlobalPosition;

			var hud = GetTree().GetFirstNodeInGroup("hud") as UI.HUD;
			hud?.ShowBossHealthBar(bossInstance, BossDisplayName);
		}

		private void OnEnemyDefeated(Enemies.Enemy enemy)
		{
			_aliveEnemyCount--;
			if (_aliveEnemyCount <= 0)
			{
				SetDoors(open: true);
				MarkThisRoomCleared();

				if (ShowVictoryOnClear)
				{
					ShowVictoryScreen();
				}
			}
		}

		private void MarkThisRoomCleared()
		{
			DungeonManager.Instance?.MarkRoomCleared(SceneFilePath);
		}

		private void ShowVictoryScreen()
		{
			GetTree().Paused = true;

			var victoryScene = GD.Load<PackedScene>("res://Scenes/UI/VictoryScreen.tscn");
			var instance = victoryScene.Instantiate();
			GetTree().Root.AddChild(instance);
		}

		private void SetDoors(bool open)
		{
			var doorsContainer = GetNodeOrNull<Node>("Doors");
			if (doorsContainer == null)
				return;

			foreach (Node child in doorsContainer.GetChildren())
			{
				if (child is Door door)
				{
					if (open)
						door.SetOpen();
					else
						door.SetClosed();
				}
			}
		}

		// Wird vom DungeonManager aufgerufen, sobald der Spieler diesen Raum betritt.
		public void OnPlayerEntered(Player.Player player)
		{
			player?.OnEnteredNewRoom();
		}
	}
}
