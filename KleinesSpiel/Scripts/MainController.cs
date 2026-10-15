using Godot;

namespace KleinesSpiel.Scripts
{
	// Sitzt auf der Wurzel von Main.tscn. Verbindet Player, RoomContainer
	// und den DungeonManager miteinander und startet im ersten Raum.
	public partial class MainController : Node
	{
		[Export] public NodePath PlayerPath;
		[Export] public NodePath RoomContainerPath;

		public override void _Ready()
		{
			var player = GetNode<Player.Player>(PlayerPath);
			var roomContainer = GetNode<Node>(RoomContainerPath);

			Rooms.DungeonManager.Instance.Initialize(roomContainer, player);
		}
        public override void _ExitTree() => Rooms.DungeonManager.Instance?.ForgetRun();
	}
}
