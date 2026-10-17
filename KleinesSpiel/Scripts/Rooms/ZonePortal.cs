using Godot;

namespace KleinesSpiel.Scripts.Rooms
{
    public partial class ZonePortal : Area2D
    {
        public string SourceRoomId;
        public int DestinationFloor;
        private float _time;
        private bool _armed;
        public override void _Ready()
        {
            CollisionLayer = 32;
            CollisionMask = 2;
            ZIndex = 5;
            AddChild(new CollisionShape2D { Name = "CollisionShape2D", Shape = new CircleShape2D { Radius = 24 } });
            BodyEntered += body =>
            {
                if (_armed && body is Player.Player) DungeonManager.Instance?.RequestNextZone(SourceRoomId);
            };
            var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
            _armed = player == null || player.GlobalPosition.DistanceTo(GlobalPosition) > 42;
            AddChild(new Label { Position = new Vector2(-140, 38), Size = new Vector2(280, 48),
                Text = DungeonFloor.NameFor(DestinationFloor), HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore });
        }
        public override void _PhysicsProcess(double delta)
        {
            if (_armed) return;
            var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (player != null && player.GlobalPosition.DistanceTo(GlobalPosition) > 42) _armed = true;
        }
        public override void _Process(double delta)
        {
            _time += (float)delta;
            Scale = Vector2.One * Mathf.Min(1, _time * 3);
            QueueRedraw();
        }
        public override void _Draw()
        {
            DrawCircle(Vector2.Zero, 27, new Color(0.1f, 0.05f, 0.2f));
            DrawCircle(Vector2.Zero, 23, new Color(0.3f, 0.1f, 0.55f, 0.7f));
            for (int ring = 0; ring < 3; ring++)
                DrawArc(Vector2.Zero, 12 + ring * 6 + Mathf.Sin(_time * 4 + ring),
                    _time * (ring + 1), _time * (ring + 1) + 5.3f, 28,
                    new Color(0.45f + ring * 0.15f, 0.45f, 1, 0.8f), 2);
            for (int i = 0; i < 8; i++)
                DrawCircle(Vector2.FromAngle(_time + i * Mathf.Tau / 8) * 29, 2, new Color(0.6f, 0.9f, 1));
        }
    }
}
