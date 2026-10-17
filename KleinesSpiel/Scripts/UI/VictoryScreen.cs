using Godot;
using KleinesSpiel.Scripts.Common;

namespace KleinesSpiel.Scripts.UI
{
	public partial class VictoryScreen : CanvasLayer
	{
		public override void _Ready()
		{
			ProcessMode = ProcessModeEnum.Always;
			MenuMotion.Configure(this);

			var retryButton = GetNode<Button>("CenterContainer/VBoxContainer/RetryButton");
			var menuButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButton");

			GetNode<Label>("CenterContainer/VBoxContainer/TitleLabel").Text = Localization.Text("Victory!", "Sieg!");
			retryButton.Text = Localization.Text("Play again", "Nochmal spielen");
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
