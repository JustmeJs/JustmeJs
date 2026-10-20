using Godot;

namespace KleinesSpiel.Scripts.Enemies
{
    // Three separate reappearances, each with one aimed arrow and a dodge window.
    public partial class BlinkArcherBoss : EnemyRanged
    {
        private enum Stage { Normal, Vanish, Aim, Recovery }
        private Stage _stage;
        private float _timer;
        private float _cooldown = 7f;
        private int _shots;
        private int _completed;
        private int _teleports;
        private Vector2 _aim;
        private Vector2 _destination;
        private Rooms.Room _room;
        private readonly RandomNumberGenerator _random = new();
        [Export] public ulong BlinkSeed;
        public override void _Ready()
        {
            base._Ready();
            BossPatterns = true;
            if (BlinkSeed == 0) _random.Randomize(); else _random.Seed = BlinkSeed;
            for (Node node = GetParent(); node != null; node = node.GetParent())
                if (node is Rooms.Room room) { _room = room; break; }
        }
        protected override void UpdateBehavior(float dt)
        {
            if (PlayerReference == null || CurrentHealth <= 0) { Velocity = Vector2.Zero; return; }
            _timer -= dt;
            QueueRedraw();
            if (_stage == Stage.Normal)
            {
                _cooldown -= dt;
                if (CanShoot && _cooldown <= 0 && FindDestination(out _destination))
                {
                    _shots = 0;
                    BeginVanish();
                    return;
                }
                base.UpdateBehavior(dt);
                return;
            }
            Velocity = Vector2.Zero;
            if (_timer > 0) return;
            switch (_stage)
            {
                case Stage.Vanish:
                    // Recheck at the moment of teleport: the player may have moved.
                    if (!SafeDestination(_destination) && !FindDestination(out _destination)) { Finish(); return; }
                    GlobalPosition = _destination;
                    _teleports++;
                    GetNode<Node2D>("Visual").Modulate = Colors.White;
                    Effects.BurstEffect.Spawn(this, GlobalPosition, new Color(0.65f, 0.5f, 1), 28, .3f);
                    _aim = GetDirectionToPlayer();
                    _stage = Stage.Aim;
                    _timer = .55f;
                    GetNode<Common.ActorAnimator>("Visual").PlayAttack(_aim);
                    break;
                case Stage.Aim:
                    Shoot(_aim);
                    _shots++;
                    _stage = Stage.Recovery;
                    _timer = .4f;
                    break;
                case Stage.Recovery:
                    if (_shots >= 3) Finish();
                    else if (FindDestination(out _destination)) BeginVanish();
                    else Finish();
                    break;
            }
        }
        private void BeginVanish()
        {
            _stage = Stage.Vanish;
            _timer = .3f;
            Velocity = Vector2.Zero;
            // Remains vulnerable and collidable during the short departure cue.
            GetNode<Node2D>("Visual").Modulate = new Color(1, 1, 1, .45f);
            Effects.BurstEffect.Spawn(this, GlobalPosition, new Color(.65f, .5f, 1), 28, .3f);
        }
        private void Finish()
        {
            _stage = Stage.Normal;
            _cooldown = _random.RandfRange(10f, 13f);
            _completed++;
            _timer = 0;
            GetNode<Node2D>("Visual").Modulate = Colors.White;
            DelayNormalShot(.8f);
        }
        private bool FindDestination(out Vector2 destination)
        {
            for (int i = 0; i < 100; i++)
            {
                Vector2 point = new(_random.RandfRange(328, 696), _random.RandfRange(104, 472));
                if (_room != null) point = _room.ToGlobal(point);
                if (SafeDestination(point)) { destination = point; return true; }
            }
            destination = GlobalPosition;
            return false;
        }
        private bool SafeDestination(Vector2 at)
        {
            if (at.DistanceTo(PlayerReference.GlobalPosition) < 160 || at.DistanceTo(GlobalPosition) < 110) return false;
            if (_room != null)
            {
                Vector2 local = _room.ToLocal(at);
                foreach (var obstacle in Rooms.RoomLayouts.Obstacles(_room.Definition.Layout))
                    if (obstacle.Grow(36).HasPoint(local)) return false;
            }
            var shape = GetNode<CollisionShape2D>("CollisionShape2D");
            var query = new PhysicsShapeQueryParameters2D { Shape = shape.Shape,
                Transform = new Transform2D(0, at), CollisionMask = 7,
                Exclude = new Godot.Collections.Array<Rid> { GetRid() } };
            if (GetWorld2D().DirectSpaceState.IntersectShape(query, 1).Count > 0) return false;
            var ray = PhysicsRayQueryParameters2D.Create(at, PlayerReference.GlobalPosition, 1);
            return GetWorld2D().DirectSpaceState.IntersectRay(ray).Count == 0;
        }
        protected override void Die()
        {
            _stage = Stage.Normal;
            GetNode<Node2D>("Visual").Modulate = Colors.White;
            QueueRedraw();
            base.Die();
        }
        public override void _Draw()
        {
            if (_stage != Stage.Aim || CurrentHealth <= 0) return;
            DrawLine(_aim * 32, _aim * 100, new Color(.8f, .6f, 1, .65f), 2);
        }
        public string GetBlinkStage() => _stage.ToString();
        public int GetBlinkShots() => _shots;
        public int GetBlinkTeleports() => _teleports;
        public int GetBlinkSequences() => _completed;
        public float GetBlinkCooldown() => _cooldown;
        public Vector2 GetBlinkAim() => _aim;
    }
}
