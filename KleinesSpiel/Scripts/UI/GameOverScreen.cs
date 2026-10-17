using Godot;
using KleinesSpiel.Scripts.Common;

namespace KleinesSpiel.Scripts.UI
{
	// Wird vom Spieler beim Tod als eigenstaendige Ueberlagerung erzeugt.
	// ProcessMode = Always, damit die Buttons auch funktionieren, waehrend
	// das Spiel pausiert ist.
	public partial class GameOverScreen : CanvasLayer
	{
		public override void _Ready()
		{
			ProcessMode = ProcessModeEnum.Always;
			MenuMotion.Configure(this);

			var retryButton = GetNode<Button>("CenterContainer/VBoxContainer/RetryButton");
			var menuButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButton");

			GetNode<Label>("CenterContainer/VBoxContainer/TitleLabel").Text = Localization.Text("Game over", "Spiel vorbei");
			retryButton.Text = Localization.Text("Retry", "Neu versuchen");
			menuButton.Text = Localization.Text("Main menu", "Hauptmenü");

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
