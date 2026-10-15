using Godot;

namespace KleinesSpiel.Scripts.Rooms
{
    // Visual geometry follows the existing collision shapes, including door gaps.
    [Tool]
    public partial class RoomArt : Node2D
    {
        private Texture2D _wall;
        private float _time;

        public override void _Process(double delta)
        {
            if (Engine.IsEditorHint()) return;
            _time += (float)delta;
            QueueRedraw();
        }

        public override void _Ready()
        {
            _wall = GD.Load<Texture2D>("res://Assets/Sprites/Tiles/stone_wall.svg");
            TextureFilter = TextureFilterEnum.Nearest;
            TextureRepeat = TextureRepeatEnum.Enabled;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_wall == null) return;
            var walls = GetParent().GetNodeOrNull<Node2D>("Walls");
            if (walls == null) return;
            foreach (var child in walls.GetChildren())
            {
                if (child is not CollisionShape2D collider || collider.Shape is not RectangleShape2D rectangle)
                    continue;
                DrawSetTransform(walls.Position + collider.Position, collider.Rotation, collider.Scale);
                DrawTextureRect(_wall, new Rect2(-rectangle.Size / 2, rectangle.Size), true);
            }
            DrawSetTransform(Vector2.Zero);
            DrawTorch(new Vector2(352, 67), 0);
            DrawTorch(new Vector2(672, 67), 1.7f);
        }

        private void DrawTorch(Vector2 at, float phase)
        {
            float flicker = Mathf.Sin(_time * 15 + phase);
            DrawCircle(at, 18 + flicker * 2, new Color(1, 0.5f, 0.15f, 0.08f));
            DrawRect(new Rect2(at + new Vector2(-3, 1), new Vector2(6, 12)), new Color(0.2f, 0.13f, 0.17f));
            DrawRect(new Rect2(at + new Vector2(-5, 1), new Vector2(10, 3)), new Color(0.6f, 0.45f, 0.3f));
            DrawRect(new Rect2(at + new Vector2(-4, -6 - flicker), new Vector2(8, 8 + flicker)), new Color(1, 0.35f, 0.12f));
            DrawRect(new Rect2(at + new Vector2(-2, -8 + flicker), new Vector2(4, 8 - flicker)), new Color(1, 0.85f, 0.35f));
            float spark = (_time * 12 + phase * 10) % 14;
            DrawRect(new Rect2(at + new Vector2(Mathf.Sin(_time * 4 + phase) * 4, -9 - spark), Vector2.One * 2),
                new Color(1, 0.7f, 0.25f, 1 - spark / 14));
        }
    }
}
