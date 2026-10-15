using Godot;

namespace KleinesSpiel.Scripts.Rooms
{
    // Visual geometry follows the existing collision shapes, including door gaps.
    [Tool]
    public partial class RoomArt : Node2D
    {
        private Texture2D _wall;

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
        }
    }
}
