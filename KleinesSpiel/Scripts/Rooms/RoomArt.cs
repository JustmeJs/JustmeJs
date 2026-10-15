using Godot;

namespace KleinesSpiel.Scripts.Rooms
{
    // Visual geometry follows the existing collision shapes, including door gaps.
    [Tool]
    public partial class RoomArt : Node2D
    {
        [Export] public Color StoneTint = Colors.White;
        [Export] public bool Amethyst;
        [Export] public RoomMapType RoomKind;
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
            foreach (string group in new[] { "Walls", "Obstacles" })
            {
                var bodies = GetParent().GetNodeOrNull<Node2D>(group);
                if (bodies == null) continue;
                foreach (var child in bodies.GetChildren())
                {
                    if (child is not CollisionShape2D collider || collider.Shape is not RectangleShape2D rectangle) continue;
                    DrawSetTransform(bodies.Position + collider.Position, collider.Rotation, collider.Scale);
                    var rect = new Rect2(-rectangle.Size / 2, rectangle.Size);
                    DrawTextureRect(_wall, rect, true, StoneTint);
                    if (group == "Obstacles")
                    {
                        DrawRect(rect, new Color(0.04f, 0.04f, 0.09f), false, 3);
                        DrawLine(rect.Position + Vector2.One * 3, rect.Position + new Vector2(rect.Size.X - 3, 3), StoneTint * 0.65f, 2);
                    }
                }
            }
            DrawSetTransform(Vector2.Zero);
            if (RoomKind is RoomMapType.Treasure or RoomMapType.Boss)
            {
                Color rune = RoomKind == RoomMapType.Treasure ? new Color(1, 0.8f, 0.3f, 0.28f)
                    : new Color(0.7f, 0.3f, 1, 0.24f);
                DrawArc(new Vector2(512,288), RoomKind == RoomMapType.Treasure ? 48 : 100,
                    _time * 0.12f, _time * 0.12f + Mathf.Tau, 48, rune, 2);
                if (RoomKind == RoomMapType.Treasure)
                    DrawRect(new Rect2(496,272,32,32), new Color(0.75f,0.6f,0.3f,0.35f));
            }
            DrawTorch(new Vector2(352, 67), 0);
            DrawTorch(new Vector2(672, 67), 1.7f);
        }

        private void DrawTorch(Vector2 at, float phase)
        {
            float flicker = Mathf.Sin(_time * 15 + phase);
            DrawCircle(at, 18 + flicker * 2, Amethyst ? new Color(0.65f, 0.3f, 1, 0.1f) : new Color(1, 0.5f, 0.15f, 0.08f));
            DrawRect(new Rect2(at + new Vector2(-3, 1), new Vector2(6, 12)), new Color(0.2f, 0.13f, 0.17f));
            DrawRect(new Rect2(at + new Vector2(-5, 1), new Vector2(10, 3)), new Color(0.6f, 0.45f, 0.3f));
            DrawRect(new Rect2(at + new Vector2(-4, -6 - flicker), new Vector2(8, 8 + flicker)), Amethyst ? new Color(0.6f, 0.25f, 1) : new Color(1, 0.35f, 0.12f));
            DrawRect(new Rect2(at + new Vector2(-2, -8 + flicker), new Vector2(4, 8 - flicker)), Amethyst ? new Color(0.8f, 0.7f, 1) : new Color(1, 0.85f, 0.35f));
            float spark = (_time * 12 + phase * 10) % 14;
            DrawRect(new Rect2(at + new Vector2(Mathf.Sin(_time * 4 + phase) * 4, -9 - spark), Vector2.One * 2),
                new Color(1, 0.7f, 0.25f, 1 - spark / 14));
        }
    }
}
