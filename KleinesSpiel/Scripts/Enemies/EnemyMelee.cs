using Godot;

namespace KleinesSpiel.Scripts.Enemies
{
    public partial class EnemyMelee : Enemy
    {
        private Rooms.Room _room;
        private float _radius = 14;
        private float _repathTimer;
        private Vector2 _waypoint;

        public override void _Ready()
        {
            base._Ready();
            for (Node node = GetParent(); node != null; node = node.GetParent())
                if (node is Rooms.Room room) { _room = room; break; }
            if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D")?.Shape is RectangleShape2D shape)
                _radius = Mathf.Max(shape.Size.X, shape.Size.Y) * 0.5f;
        }

        protected override void UpdateBehavior(float dt)
        {
            if (_room == null || PlayerReference == null)
            {
                Velocity = GetDirectionToPlayer() * MoveSpeed;
                return;
            }
            _repathTimer -= dt;
            if (_repathTimer <= 0 || Position.DistanceTo(_waypoint) < 5 || GetSlideCollisionCount() > 0)
            {
                _waypoint = _room.GetEnemyWaypoint(Position, _room.ToLocal(PlayerReference.GlobalPosition), _radius);
                _repathTimer = 0.2f;
            }
            Vector2 direction = _waypoint - Position;
            Velocity = direction.Length() > 2 ? direction.Normalized() * MoveSpeed : Vector2.Zero;
        }
    }
}
