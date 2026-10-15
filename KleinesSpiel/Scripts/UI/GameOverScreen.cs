using Godot;

namespace KleinesSpiel.Scripts.UI
{
	// Wird vom Spieler beim Tod als eigenstaendige Ueberlagerung erzeugt.
	// ProcessMode = Always, damit die Buttons auch funktionieren, waehrend
	// das Spiel pausiert ist.
	public partial class GameOverScreen : CanvasLayer
	{
		public override void _Ready()
		{
			MenuMotion.Configure(this);
			ProcessMode = ProcessModeEnum.Always;

			var retryButton = GetNode<Button>("CenterContainer/VBoxContainer/RetryButton");
			var menuButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButton");

			retryButton.Pressed += OnRetryPressed;
			menuButton.Pressed += OnMenuPressed;
		}

		private void OnRetryPressed()
		{
			GetTree().Paused = false;
			GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
			QueueFree();
		}

		private void OnMenuPressed()
		{
			GetTree().Paused = false;
			GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");
			QueueFree();
		}
	}
}
