using Godot;

namespace KleinesSpiel.Scripts.UI
{
    // Reads the same half-heart values as the existing HUD; no gameplay changes.
    public partial class HealthIcons : Control
    {
        private Texture2D _heart;
        private Player.Player _player;
        private int _current = -1;
        private int _maximum = -1;

        public override void _Ready()
        {
            _heart = GD.Load<Texture2D>("res://Resources/Art/heart.tres");
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Process(double delta)
        {
            if (!IsInstanceValid(_player))
                _player = GetTree().GetFirstNodeInGroup("player") as Player.Player;
            if (_player == null) return;
            int current = _player.GetCurrentHealthHalfHearts();
            int maximum = _player.GetMaxHealthHalfHearts();
            if (current == _current && maximum == _maximum) return;
            _current = current;
            _maximum = maximum;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_heart == null) return;
            for (int i = 0; i < (_maximum + 1) / 2; i++)
            {
                var position = new Vector2((i % 7) * 30, (i / 7) * 30);
                var rect = new Rect2(position, new Vector2(28, 28));
                DrawTextureRect(_heart, rect, false, new Color(0.25f, 0.25f, 0.3f));
                int halves = Mathf.Clamp(_current - i * 2, 0, 2);
                if (halves == 2)
                    DrawTextureRect(_heart, rect, false);
                else if (halves == 1)
                    DrawTextureRectRegion(_heart, new Rect2(position, new Vector2(14, 28)),
                        new Rect2(0, 0, _heart.GetWidth() / 2f, _heart.GetHeight()));
            }
        }
    }
}
