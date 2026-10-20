using Godot;
using KleinesSpiel.Scripts.Common;

namespace KleinesSpiel.Scripts.UI
{
    // A modal child of the owning menu: Escape returns to that menu without unpausing.
    public partial class SettingsMenu : CanvasLayer
    {
        private Control _ownerContent;
        private Button _returnFocus;
        public static void Open(Node owner, Button button)
        {
            if (owner.HasNode("SettingsMenu")) return;
            var menu = new SettingsMenu { Name = "SettingsMenu", Layer = 100,
                _ownerContent = owner.GetNode<Control>("CenterContainer"), _returnFocus = button };
            menu._ownerContent.Hide();
            owner.AddChild(menu);
        }
        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
            AddToGroup("settings_menu");
            var background = new ColorRect { Color = new Color(0.025f, 0.035f, 0.055f, 0.98f) };
            AddChild(background); background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var center = new CenterContainer { Name = "Center", Theme = GD.Load<Theme>("res://Resources/Art/dungeon_theme.tres") };
            AddChild(center); center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var box = new VBoxContainer { Name = "Options", CustomMinimumSize = new Vector2(450, 0) };
            box.AddThemeConstantOverride("separation", 20); center.AddChild(box);
            var title = new Label { Text = Localization.Text("Settings", "Einstellungen"), HorizontalAlignment = HorizontalAlignment.Center };
            title.AddThemeFontSizeOverride("font_size", 32); box.AddChild(title);
            var audio = Audio.GameAudio.Instance;
            var music = AddSlider(box, "Music", Localization.Text("Music", "Musik"), audio.GetMusicVolume(), audio.SetMusicVolume);
            var effects = AddSlider(box, "Effects", Localization.Text("Game sounds", "Spielgeräusche"), audio.GetEffectsVolume(), audio.SetEffectsVolume);
            var hint = new Label { Text = Localization.Text("↑ / ↓: select   ← / →: volume", "↑ / ↓: wählen   ← / →: Lautstärke"), HorizontalAlignment = HorizontalAlignment.Center };
            box.AddChild(hint);
            var back = new Button { Name = "Back", Text = Localization.Text("Back", "Zurück"), CustomMinimumSize = new Vector2(0, 50) };
            box.AddChild(back);
            back.Pressed += Close;
            Control[] controls = { music, effects, back };
            for (int i = 0; i < controls.Length; i++)
            {
                var c = controls[i];
                c.FocusMode = Control.FocusModeEnum.All;
                c.FocusNeighborTop = c.FocusPrevious = c.GetPathTo(controls[(i + 2) % 3]);
                c.FocusNeighborBottom = c.FocusNext = c.GetPathTo(controls[(i + 1) % 3]);
                c.FocusNeighborLeft = c.FocusNeighborRight = c.GetPathTo(c);
            }
            music.CallDeferred(Control.MethodName.GrabFocus);
        }
        private HSlider AddSlider(VBoxContainer box, string name, string title, float value, System.Action<float> changed)
        {
            var label = new Label { Name = name + "Label", Text = $"{title}: {Mathf.RoundToInt(value * 100)}%" };
            box.AddChild(label);
            var slider = new HSlider { Name = name, MinValue = 0, MaxValue = 100, Step = 5,
                Value = Mathf.RoundToInt(value * 100), CustomMinimumSize = new Vector2(420, 32) };
            box.AddChild(slider);
            slider.ValueChanged += percent => { label.Text = $"{title}: {percent:0}%"; changed((float)percent / 100); };
            return slider;
        }
        public override void _Input(InputEvent @event)
        {
            if (!@event.IsActionPressed("ui_cancel") || @event.IsEcho()) return;
            GetViewport().SetInputAsHandled();
            Close();
        }
        private void Close() { Input.ActionRelease("dash"); QueueFree(); }
        public override void _ExitTree()
        {
            if (IsInstanceValid(_ownerContent) && _ownerContent.IsInsideTree() && !GetParent().IsQueuedForDeletion())
            {
                _ownerContent.Show();
                if (IsInstanceValid(_returnFocus)) _returnFocus.CallDeferred(Control.MethodName.GrabFocus);
            }
        }
    }
}
