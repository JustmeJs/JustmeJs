using Godot;

namespace KleinesSpiel.Scripts
{
	// Sitzt auf der Wurzel von Main.tscn. Verbindet Player, RoomContainer
	// und den DungeonManager miteinander und startet im ersten Raum.
	public partial class MainController : Node
	{
		[Export] public NodePath PlayerPath;
		[Export] public NodePath RoomContainerPath;
		[Export] public string StartRoomScenePath = "res://Scenes/Rooms/StartRoom.tscn";
		[Export] public string StartSpawnPointName = "SpawnDefault";

		public override void _Ready()
		{
			var player = GetNode<Player.Player>(PlayerPath);
			var roomContainer = GetNode<Node>(RoomContainerPath);

			Rooms.DungeonManager.Instance.Initialize(roomContainer, player);
			Rooms.DungeonManager.Instance.ChangeRoom(StartRoomScenePath, StartSpawnPointName);
		}
	}
}
