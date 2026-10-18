using Godot;

namespace KleinesSpiel.Scripts.Items
{
    public partial class PickupMotion : Node2D
    {
        [Export] public bool IsHeart;
        private Sprite2D _sprite;
        private Vector2 _baseScale;
        private float _time;
        private float _collectedTime;
        private bool _collected;

        public override void _Ready()
        {
            _sprite = GetNodeOrNull<Sprite2D>("Sprite");
            if (_sprite != null) _baseScale = _sprite.Scale;
        }

        public void Collect()
        {
            if (_collected) return;
            _collected = true;
            Effects.BurstEffect.Spawn(this, GlobalPosition,
                IsHeart ? new Color(1, 0.25f, 0.4f) : new Color(1, 0.85f, 0.35f), 24, 0.35f);
        }

        public override void _Process(double delta)
        {
            _time += (float)delta;
            if (_sprite == null) return;
            if (_collected)
            {
                _collectedTime += (float)delta;
                float t = Mathf.Clamp(_collectedTime / 0.28f, 0, 1);
                Position = new Vector2(0, -30 * t);
                _sprite.Scale = _baseScale * (1 + t * 0.7f);
                Modulate = new Color(1, 1, 1, 1 - t);
                if (t >= 1) GetParent().QueueFree();
            }
            else
            {
                Position = new Vector2(0, -3 - Mathf.Sin(_time * 3) * 3);
                float pulse = IsHeart ? 1 + Mathf.Pow(Mathf.Max(0, Mathf.Sin(_time * 7)), 6) * 0.12f
                    : 1 + Mathf.Sin(_time * 3) * 0.045f;
                _sprite.Scale = _baseScale * pulse;
                _sprite.Rotation = IsHeart ? 0 : Mathf.Sin(_time * 2) * 0.055f;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_collected) return;
            Color color = IsHeart ? new Color(1, 0.3f, 0.4f, 0.65f) : new Color(1, 0.85f, 0.4f, 0.75f);
            for (int i = 0; i < 3; i++)
            {
                float angle = _time * 1.5f + i * Mathf.Tau / 3;
                Vector2 at = new Vector2(Mathf.Cos(angle) * (IsHeart ? 12 : 19), Mathf.Sin(angle) * (IsHeart ? 8 : 12));
                float brightness = 0.5f + 0.5f * Mathf.Sin(_time * 5 + i);
                DrawLine(at - Vector2.Right * 2, at + Vector2.Right * 2, color * brightness);
                DrawLine(at - Vector2.Down * 2, at + Vector2.Down * 2, color * brightness);
            }
        }
    }
}
