using Godot;
using System.Linq;

namespace KleinesSpiel.Scripts.UI
{
    public static class MenuMotion
    {
        public static void Configure(Node root)
        {
            foreach (Node child in root.GetChildren())
            {
                if (child is Control control)
                {
                    control.Modulate = new Color(1, 1, 1, 0);
                    control.CreateTween().TweenProperty(control, "modulate:a", 1, 0.22f);
                }
            }
            var buttons = root.FindChildren("*", "Button").OfType<Button>()
                .Where(button => !button.Disabled && button.Visible).ToArray();
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                button.FocusMode = Control.FocusModeEnum.All;
                var previous = button.GetPathTo(buttons[(i + buttons.Length - 1) % buttons.Length]);
                var next = button.GetPathTo(buttons[(i + 1) % buttons.Length]);
                button.FocusNeighborTop = previous;
                button.FocusNeighborLeft = previous;
                button.FocusPrevious = previous;
                button.FocusNeighborBottom = next;
                button.FocusNeighborRight = next;
                button.FocusNext = next;
                Tween active = null;
                void Update()
                {
                    active?.Kill();
                    button.PivotOffset = button.Size / 2;
                    bool highlighted = button.IsHovered() || button.HasFocus();
                    active = button.CreateTween();
                    active.TweenProperty(button, "scale", Vector2.One * (highlighted ? 1.045f : 1), 0.1f);
                }
                button.MouseEntered += Update;
                button.MouseExited += Update;
                button.FocusEntered += Update;
                button.FocusExited += Update;
                // Space confirms buttons and is also the gameplay dash key. Consume the
                // gameplay action before a menu resumes or starts a run.
                button.Pressed += () => Input.ActionRelease("dash");
            }
            if (buttons.Length > 0) buttons[0].CallDeferred(Control.MethodName.GrabFocus);
        }
    }
}
