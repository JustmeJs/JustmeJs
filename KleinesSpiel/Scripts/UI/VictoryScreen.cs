using Godot;

namespace KleinesSpiel.Scripts.UI
{
	public partial class VictoryScreen : CanvasLayer
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
