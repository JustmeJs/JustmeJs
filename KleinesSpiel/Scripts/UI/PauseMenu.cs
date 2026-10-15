using Godot;

namespace KleinesSpiel.Scripts.UI
{
	// Wird von MainController erzeugt, sobald "pause" (Escape) gedrueckt wird.
	// ProcessMode = Always, damit die Buttons auch funktionieren, waehrend
	// das Spiel pausiert ist.
	public partial class PauseMenu : CanvasLayer
	{
		// MainController hoert darauf, um seine eigene Referenz zu loeschen,
		// egal ob ueber "Weiterspielen" oder erneutes Escape geschlossen wurde.
		public event System.Action Closed;

		public override void _Ready()
		{
			ProcessMode = ProcessModeEnum.Always;

			var continueButton = GetNode<Button>("CenterContainer/VBoxContainer/ContinueButton");
			var retryButton = GetNode<Button>("CenterContainer/VBoxContainer/RetryButton");
			var menuButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButton");

			continueButton.Pressed += OnContinuePressed;
			retryButton.Pressed += OnRetryPressed;
			menuButton.Pressed += OnMenuPressed;
		}

		private void OnContinuePressed()
		{
			GetTree().Paused = false;
			Closed?.Invoke();
			QueueFree();
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
