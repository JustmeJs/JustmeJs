using Godot;

namespace KleinesSpiel.Scripts.Rooms
{
	// Waehlt beim Start zufaellig eines der 5 passiven Items aus dem Pool
	// und platziert es als abholbaren ItemPickup an dieser Position.
	// Die Pfade sind fest im Code hinterlegt, damit keine Array-Zuweisung
	// im .tscn-Editor noetig ist (weniger Fehlerquellen).
	public partial class TreasureItemSpawner : Node2D
	{
		[Export] public PackedScene ItemPickupScene;

		private static readonly string[] ItemResourcePaths =
		{
			"res://Resources/Items/Item01_ExtraHeart.tres",
			"res://Resources/Items/Item02_MoveSpeed.tres",
			"res://Resources/Items/Item03_FireRate.tres",
			"res://Resources/Items/Item04_Damage.tres",
			"res://Resources/Items/Item05_Piercing.tres"
		};

		public override void _Ready()
		{
			// Der Elternknoten ist die TreasureRoom selbst (Wurzel ihrer Szene),
			// daher traegt SIE den Szenen-Pfad, nicht der Spawner.
			var room = GetParent();
			string roomScenePath = room?.SceneFilePath;

			if (DungeonManager.Instance != null && DungeonManager.Instance.IsTreasureCollected(roomScenePath))
			{
				// Item in diesem Durchlauf schon abgeholt - der Raum bleibt leer.
				return;
			}

			if (ItemPickupScene == null)
			{
				GD.PrintErr("TreasureItemSpawner: ItemPickupScene ist nicht zugewiesen.");
				return;
			}

			var rng = new RandomNumberGenerator();
			rng.Randomize();
			int index = rng.RandiRange(0, ItemResourcePaths.Length - 1);

			var chosenItem = GD.Load<Items.ItemData>(ItemResourcePaths[index]);
			if (chosenItem == null)
			{
				GD.PrintErr($"TreasureItemSpawner: Item-Ressource nicht gefunden: {ItemResourcePaths[index]}");
				return;
			}

			var pickupInstance = ItemPickupScene.Instantiate<Items.ItemPickup>();
			pickupInstance.Data = chosenItem;
			pickupInstance.Collected += () => DungeonManager.Instance?.MarkTreasureCollected(roomScenePath);

			// WICHTIG: Waehrend dieses _Ready() laeuft, baut TreasureRoom (der
			// Elternknoten) noch seine eigene Kinderliste auf - er ist fuer
			// add_child() "busy". Ein direkter, synchroner AddChild()-Aufruf
			// wird von Godot in diesem Moment stillschweigend verworfen (nur
			// eine Warnung im Output, KEINE C#-Exception), weshalb der Rest
			// der Methode trotzdem "erfolgreich" weiterlief und der Log
			// korrekt aussah, obwohl der Knoten nie im SceneTree landete.
			// Loesung: add_child ueber CallDeferred ausfuehren, damit er erst
			// laeuft, sobald der Baum nicht mehr "busy" ist.
			var parent = GetParent();
			Vector2 spawnPosition = GlobalPosition;

			parent.CallDeferred(Node.MethodName.AddChild, pickupInstance);
			CallDeferred(MethodName.FinishSpawn, pickupInstance, spawnPosition, chosenItem.ItemName);
		}

		// Laeuft garantiert NACH dem deferred AddChild (gleiche Aufrufreihenfolge
		// in der deferred-Queue), also ist pickupInstance hier sicher im Baum.
		private void FinishSpawn(Items.ItemPickup pickupInstance, Vector2 spawnPosition, string itemName)
		{
			pickupInstance.GlobalPosition = spawnPosition;

			GD.Print($"TreasureItemSpawner: '{itemName}' erzeugt bei {pickupInstance.GlobalPosition}");
		}
	}
}

