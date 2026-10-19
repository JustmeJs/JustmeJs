using Godot;

namespace KleinesSpiel.Scripts.Enemies
{
    public partial class ChargingMeleeBoss : EnemyMelee
    {
        private enum Stage { Chase, Windup, Charge, Recover }
        private Stage _stage;
        private float _timer = 3;
        private readonly RandomNumberGenerator _attackRandom = new();
        public override void _Ready() { base._Ready(); _attackRandom.Randomize(); _timer = _attackRandom.RandfRange(2.4f, 4.2f); }
        private Vector2 _aim;
        public string GetChargeStage() => _stage.ToString();
        public Vector2 GetChargeAim() => _aim;
        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            if (_stage == Stage.Charge && GetSlideCollisionCount() > 0) Recover();
        }
        protected override void UpdateBehavior(float dt)
        {
            _timer -= dt;
            switch (_stage)
            {
                case Stage.Chase:
                    base.UpdateBehavior(dt);
                    if (_timer <= 0 && CanShoot && PlayerReference != null)
                    {
                        _stage = Stage.Windup; _timer = 0.8f;
                        _aim = GetDirectionToPlayer(); Velocity = Vector2.Zero;
                        GetNode<Common.ActorAnimator>("Visual").PlayAttack(_aim);
                        Audio.GameAudio.Instance?.PlayEffect("charge");
                    }
                    break;
                case Stage.Windup:
                    Velocity = Vector2.Zero;
                    GetNode<Node2D>("Visual").Rotation = Mathf.Sin(_timer * 65) * 0.05f;
                    if (_timer <= 0)
                    {
                        _stage = Stage.Charge; _timer = 0.7f;
                        GetNode<Node2D>("Visual").Rotation = 0;
                        GetNode<Common.ActorAnimator>("Visual").PlayDash(0.7f);
                    }
                    break;
                case Stage.Charge:
                    Velocity = _aim * 340;
                    if (_timer <= 0) Recover();
                    break;
                case Stage.Recover:
                    Velocity = Vector2.Zero;
                    if (_timer <= 0) { _stage = Stage.Chase; _timer = _attackRandom.RandfRange(2.4f, 4.2f) * ShotIntervalMultiplier; }
                    break;
            }
            QueueRedraw();
        }
        private void Recover() { _stage = Stage.Recover; _timer = 0.65f; Velocity = Vector2.Zero; QueueRedraw(); }
        public override void _Draw()
        {
            if (_stage != Stage.Windup || CurrentHealth <= 0) return;
            var side = _aim.Orthogonal() * 32;
            DrawColoredPolygon(new[] { side, side + _aim * 230, -side + _aim * 230, -side }, new Color(1, 0.35f, 0.15f, 0.2f));
            DrawLine(Vector2.Zero, _aim * 230, new Color(1, 0.65f, 0.2f), 3);
        }
    }
}
