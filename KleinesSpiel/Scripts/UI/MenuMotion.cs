using Godot;

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
            foreach (var node in root.FindChildren("*", "Button"))
            {
                if (node is not Button button) continue;
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
            }
        }
    }
}
