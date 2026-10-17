using Godot;

namespace KleinesSpiel.Scripts.Items
{
    public partial class CoinPickup : Area2D
    {
        public int Value = 1;
        private bool _collected;
        private float _time;
        public override void _Ready()
        {
            CollisionLayer = 32;
            CollisionMask = 2;
            ZIndex = 8;
            AddToGroup("coins");
            AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 10 } });
            BodyEntered += body =>
            {
                if (_collected || body is not Player.Player player) return;
                int added = player.AddCoins(Value);
                if (added == 0) return;
                Value -= added;
                if (Value > 0) return;
                _collected = true;
                SetDeferred(Area2D.PropertyName.Monitoring, false);
                Effects.BurstEffect.Spawn(this, GlobalPosition, new Color(1, 0.8f, 0.2f), 14, 0.22f);
                QueueFree();
            };
        }
        public override void _Process(double delta) { _time += (float)delta; QueueRedraw(); }
        public override void _Draw()
        {
            float bob = Mathf.Sin(_time * 3) * 2;
            if (Value > 1)
            {
                DrawCircle(new Vector2(-4, 2 + bob), 8, new Color(0.7f, 0.4f, 0.1f));
                DrawCircle(new Vector2(4, 1 + bob), 8, new Color(0.9f, 0.6f, 0.15f));
            }
            DrawCircle(new Vector2(0, bob), 8, new Color(0.22f, 0.13f, 0.04f));
            DrawCircle(new Vector2(0, bob), 6.5f, new Color(1, 0.78f, 0.2f));
            DrawArc(new Vector2(0, bob), 4.5f, 0, Mathf.Tau, 16, new Color(0.7f, 0.4f, 0.1f), 1);
            DrawLine(new Vector2(0, bob - 3), new Vector2(0, bob + 3), new Color(1, 0.95f, 0.6f), 2);
        }
    }
}
