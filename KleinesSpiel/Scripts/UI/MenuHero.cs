using Godot;

namespace KleinesSpiel.Scripts.UI
{
    public partial class MenuHero : TextureRect
    {
        private SpriteFrames _frames;
        private float _time;

        public override void _Ready()
        {
            _frames = GD.Load<SpriteFrames>("res://Resources/Animations/hero_frames.tres");
        }

        public override void _Process(double delta)
        {
            if (_frames == null) return;
            _time += (float)delta;
            Texture = _frames.GetFrameTexture("idle", (int)(_time * 3) % 2);
            PivotOffset = Size / 2;
            Scale = Vector2.One * (1 + Mathf.Sin(_time * 3) * 0.025f);
        }
    }
}
