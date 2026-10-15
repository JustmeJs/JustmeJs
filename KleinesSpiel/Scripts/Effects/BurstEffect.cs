using Godot;

namespace KleinesSpiel.Scripts.Effects
{
    public partial class BurstEffect : Node2D
    {
        private Color _color;
        private float _radius;
        private float _duration;
        private float _age;

        public static BurstEffect Spawn(Node source, Vector2 position, Color color,
            float radius = 14, float duration = 0.25f)
        {
            var parent = source.GetTree()?.CurrentScene;
            if (parent == null) return null;
            var effect = new BurstEffect
            {
                _color = color, _radius = radius, _duration = duration, ZIndex = 20
            };
            parent.AddChild(effect);
            effect.GlobalPosition = position;
            return effect;
        }

        public static void Ghost(Node source, AnimatedSprite2D sprite, Color color)
        {
            var parent = source.GetTree()?.CurrentScene;
            if (parent == null || sprite.SpriteFrames == null) return;
            var ghost = new Sprite2D
            {
                Texture = sprite.SpriteFrames.GetFrameTexture(sprite.Animation, sprite.Frame),
                FlipH = sprite.FlipH, Modulate = new Color(color, 0.5f), ZIndex = -1
            };
            parent.AddChild(ghost);
            ghost.GlobalTransform = sprite.GlobalTransform;
            ghost.TextureFilter = TextureFilterEnum.Nearest;
            var tween = ghost.CreateTween();
            tween.TweenProperty(ghost, "modulate:a", 0, 0.18f);
            tween.TweenCallback(Callable.From(ghost.QueueFree));
        }

        public override void _Process(double delta)
        {
            _age += (float)delta;
            if (_age >= _duration) QueueFree();
            else QueueRedraw();
        }

        public override void _Draw()
        {
            float t = Mathf.Clamp(_age / _duration, 0, 1);
            var color = new Color(_color, 1 - t);
            DrawArc(Vector2.Zero, _radius * t, 0, Mathf.Tau, 20, color, 1.5f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 direction = Vector2.FromAngle(i * Mathf.Tau / 8 + 0.3f);
                DrawLine(direction * _radius * t, direction * _radius * (t + 0.2f), color, 2);
            }
        }
    }
}
