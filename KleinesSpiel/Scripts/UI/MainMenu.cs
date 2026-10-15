using Godot;

namespace KleinesSpiel.Scripts.UI
{
	public partial class MainMenu : Control
	{
		public override void _Ready()
		{
			MenuMotion.Configure(this);
			var playButton = GetNode<Button>("CenterContainer/VBoxContainer/PlayButton");
			playButton.Pressed += OnPlayPressed;
		}

		private void OnPlayPressed()
		{
			GetTree().Paused = false;
			GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
		}
	}
}
