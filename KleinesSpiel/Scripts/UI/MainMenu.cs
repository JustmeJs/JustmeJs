using Godot;
using KleinesSpiel.Scripts.Common;

namespace KleinesSpiel.Scripts.UI
{
	public partial class MainMenu : Control
	{
		public override void _Ready()
		{
			MenuMotion.Configure(this);
			var playButton = GetNode<Button>("CenterContainer/VBoxContainer/PlayButton");
			playButton.Pressed += OnPlayPressed;
			GetNode<Button>("CenterContainer/VBoxContainer/LanguageButton").Pressed += OnLanguagePressed;
			RefreshText();
		}

		public string GetLanguageCode() => Localization.Language;

		private void OnLanguagePressed()
		{
			Localization.ToggleLanguage();
			RefreshText();
		}

		private void RefreshText()
		{
			GetNode<Button>("CenterContainer/VBoxContainer/PlayButton").Text = Localization.Text("Play", "Spielen");
			GetNode<Button>("CenterContainer/VBoxContainer/LanguageButton").Text = Localization.Text("Language: English", "Sprache: Deutsch");
			GetNode<Label>("CenterContainer/VBoxContainer/HelpLabel").Text = Localization.Text(
				"Arrow keys: select   Enter / Space: confirm", "Pfeiltasten: wählen   Enter / Leertaste: bestätigen");
		}

		private void OnPlayPressed()
		{
			GetTree().Paused = false;
			GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
		}
	}
}
