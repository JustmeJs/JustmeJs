using Godot;

namespace KleinesSpiel.Scripts.Enemies
{
    // The final encounter has readable windups, short attack windows and recovery.
    // A charge always commits to the displayed direction; it never tracks mid-dash.
    public partial class CastleBoss : Enemy
    {
        [Export] public PackedScene ProjectileScene;
        [Export] public float ProjectileDamage = 1f;

        private enum Pattern { Fan, Ring, Charge }
        private enum Stage { Recovery, Telegraph, Attacking, Defeated }
        private Pattern _pattern;
        private Stage _stage = Stage.Recovery;
        private Vector2 _arenaCenter;
        private Vector2 _lockedAim = Vector2.Down;
        private float _timer = 1.1f;
        private float _shotTimer;
        private float _visualTime;
        private int _volley;
        private int _combatPhase = 1;
        private int _nextPattern;
        private readonly int[] _attackCounts = new int[3];
        private int _totalShots;
        private bool _dead;
        private string _projectileGroup;

        public override void _Ready()
        {
            base._Ready();
            _arenaCenter = GlobalPosition;
            _projectileGroup = $"regent_projectiles_{GetInstanceId()}";
            AddToGroup("final_boss");
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            // Inspect this movement's collisions immediately. Checking IsOnWall
            // at the next behavior update is too late when contact recoil pauses
            // that update, and could restart a charge after hitting the player.
            if (!_dead && _stage == Stage.Attacking && _pattern == Pattern.Charge
                && GetSlideCollisionCount() > 0)
                BeginRecovery();
        }

        protected override void UpdateBehavior(float dt)
        {
            Velocity = Vector2.Zero;
            if (_dead || PlayerReference == null) return;
            _visualTime += dt;
            _timer -= dt;
            _combatPhase = CurrentHealth <= MaxHealth * 0.5f ? 2 : 1;
            QueueRedraw();

            switch (_stage)
            {
                case Stage.Recovery:
                    // A slow return gives the next ring and charge room to breathe.
                    Vector2 toCenter = _arenaCenter - GlobalPosition;
                    if (toCenter.Length() > 8f) Velocity = toCenter.Normalized() * 65f;
                    if (_timer <= 0f) BeginTelegraph();
                    break;
                case Stage.Telegraph:
                    if (_timer <= 0f) BeginAttack();
                    break;
                case Stage.Attacking:
                    UpdateAttack(dt);
                    break;
            }
        }

        private void BeginTelegraph()
        {
            _pattern = (Pattern)(_nextPattern++ % 3);
            _stage = Stage.Telegraph;
            _lockedAim = GetDirectionToPlayer();
            if (_lockedAim == Vector2.Zero) _lockedAim = Vector2.Down;
            _timer = _pattern == Pattern.Charge ? 0.9f : 0.7f;
            if (_combatPhase == 2) _timer -= 0.1f;
            Velocity = Vector2.Zero;
        }

        private void BeginAttack()
        {
            _stage = Stage.Attacking;
            _attackCounts[(int)_pattern]++;
            _volley = 0;
            _shotTimer = 0f;
            _timer = _pattern == Pattern.Charge ? 0.55f : 1.2f;
            AnimateAttack(_lockedAim);
        }

        private void UpdateAttack(float dt)
        {
            if (_pattern == Pattern.Charge)
            {
                // Fixed arena bounds keep a missed charge away from doorways even
                // when this enemy is tested or previewed without physical walls.
                float speed = _combatPhase == 2 ? 360f : 310f;
                Vector2 next = GlobalPosition + _lockedAim * speed * dt;
                Vector2 relative = next - _arenaCenter;
                bool touchesWall = Mathf.Abs(relative.X) > 180f || Mathf.Abs(relative.Y) > 180f;
                if (_timer <= 0f || touchesWall)
                {
                    BeginRecovery();
                    return;
                }
                Velocity = _lockedAim * speed;
                return;
            }

            _shotTimer -= dt;
            if (_shotTimer > 0f) return;
            int volleys = _pattern == Pattern.Fan ? (_combatPhase == 2 ? 3 : 2)
                : (_combatPhase == 2 ? 2 : 1);
            if (_volley >= volleys)
            {
                BeginRecovery();
                return;
            }
            AnimateAttack(_lockedAim);
            if (_pattern == Pattern.Fan) FireFan();
            else FireRing();
            _volley++;
            _shotTimer = _pattern == Pattern.Fan ? (_combatPhase == 2 ? 0.32f : 0.43f) : 0.55f;
        }

        private void FireFan()
        {
            int count = _combatPhase == 2 ? 7 : 5;
            float spread = Mathf.DegToRad(_combatPhase == 2 ? 70f : 56f);
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Lerp(-spread * 0.5f, spread * 0.5f, i / (float)(count - 1));
                SpawnProjectile(_lockedAim.Rotated(angle), _combatPhase == 2 ? 205f : 185f);
            }
        }

        private void FireRing()
        {
            int count = _combatPhase == 2 ? 24 : 18;
            // The green wedge shown during the windup stays clear for every ring.
            // Its 72-degree opening is generous enough to reach from either side.
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Vector2.Right.Rotated(Mathf.Tau * i / count);
                if (Mathf.Abs(_lockedAim.AngleTo(direction)) < Mathf.DegToRad(36f)) continue;
                SpawnProjectile(direction, _combatPhase == 2 ? 165f : 145f);
            }
        }

        private void SpawnProjectile(Vector2 direction, float speed)
        {
            if (_dead || ProjectileScene == null) return;
            var projectile = ProjectileScene.Instantiate<Projectiles.Projectile>();
            projectile.LifetimeSeconds = 2.8f;
            projectile.Position = GlobalPosition + direction * 37f;
            projectile.AddToGroup(_projectileGroup);
            // Shots are siblings of the arena so they remain in world coordinates.
            GetTree().CurrentScene.AddChild(projectile);
            projectile.GlobalPosition = GlobalPosition + direction * 37f;
            projectile.Modulate = _pattern == Pattern.Ring
                ? new Color(0.8f, 0.65f, 1f) : new Color(1f, 0.82f, 0.45f);
            projectile.Launch(direction, speed, ProjectileDamage);
            _totalShots++;
        }

        private void BeginRecovery()
        {
            _stage = Stage.Recovery;
            _timer = _combatPhase == 2 ? 0.75f : 1.05f;
            Velocity = Vector2.Zero;
        }

        protected override void Die()
        {
            _dead = true;
            _stage = Stage.Defeated;
            RemoveFromGroup("final_boss");
            foreach (Node node in GetTree().GetNodesInGroup(_projectileGroup))
            {
                if (node is not Projectiles.Projectile projectile) continue;
                projectile.SetPhysicsProcess(false);
                projectile.CollisionLayer = 0;
                projectile.CollisionMask = 0;
                projectile.QueueFree();
            }
            QueueRedraw();
            base.Die();
        }

        public override void _Draw()
        {
            if (_dead) return;
            Color gold = new Color(1f, 0.78f, 0.25f);
            Color aura = _combatPhase == 2 ? new Color(1f, 0.3f, 0.65f, 0.55f)
                : new Color(0.75f, 0.5f, 1f, 0.4f);
            float radius = 39f + Mathf.Sin(_visualTime * 3f) * 2f;
            DrawArc(Vector2.Zero, radius, 0, Mathf.Tau, 48, aura, 3f, true);
            for (int i = 0; i < 6; i++)
            {
                Vector2 orbit = Vector2.Right.Rotated(_visualTime * 0.5f + i * Mathf.Tau / 6f) * radius;
                DrawCircle(orbit, _combatPhase == 2 ? 3f : 2f, gold);
            }

            // A crown above the animated head gives this reused creature a new silhouette.
            Vector2[] crown = { new(-19, -35), new(-22, -50), new(-10, -43), new(0, -57),
                new(10, -43), new(22, -50), new(19, -35) };
            DrawColoredPolygon(crown, gold);
            DrawPolyline(new[] { crown[0], crown[1], crown[2], crown[3], crown[4], crown[5],
                crown[6], crown[0] }, new Color(0.35f, 0.2f, 0.08f), 2f, true);
            DrawCircle(new Vector2(0, -43), 4, new Color(0.7f, 0.22f, 0.9f));

            if (_stage != Stage.Telegraph) return;
            float pulse = 0.45f + (Mathf.Sin(_visualTime * 18f) + 1f) * 0.15f;
            Color warning = new Color(1f, 0.6f, 0.2f, pulse);
            if (_pattern == Pattern.Charge)
            {
                Vector2 side = _lockedAim.Orthogonal() * 22f;
                Vector2 end = _lockedAim * 190f;
                DrawColoredPolygon(new[] { side, end + side, end - side, -side },
                    new Color(1f, 0.3f, 0.3f, 0.2f));
                DrawLine(Vector2.Zero, end, warning, 4f, true);
                DrawLine(end, end - _lockedAim.Rotated(0.65f) * 20f, warning, 3f, true);
                DrawLine(end, end - _lockedAim.Rotated(-0.65f) * 20f, warning, 3f, true);
            }
            else if (_pattern == Pattern.Fan)
            {
                float angle = Mathf.DegToRad(_combatPhase == 2 ? 35f : 28f);
                DrawLine(Vector2.Zero, _lockedAim.Rotated(-angle) * 125f, warning, 2f, true);
                DrawLine(Vector2.Zero, _lockedAim.Rotated(angle) * 125f, warning, 2f, true);
                DrawArc(Vector2.Zero, 125f, _lockedAim.Angle() - angle,
                    _lockedAim.Angle() + angle, 24, warning, 2f, true);
            }
            else
            {
                float gap = Mathf.DegToRad(36f);
                float start = _lockedAim.Angle() + gap;
                DrawArc(Vector2.Zero, 85f, start, start + Mathf.Tau - gap * 2, 48, warning, 3f, true);
                Color safe = new Color(0.3f, 1f, 0.75f, pulse);
                DrawLine(Vector2.Zero, _lockedAim.Rotated(-gap) * 120f, safe, 2f, true);
                DrawLine(Vector2.Zero, _lockedAim.Rotated(gap) * 120f, safe, 2f, true);
            }
        }

        // Read-only diagnostics let regression tests observe the real combat loop.
        public int GetCombatPhase() => _combatPhase;
        public string GetAttackPattern() => _pattern.ToString();
        public string GetAttackStage() => _stage.ToString();
        public float GetAttackTimeRemaining() => _timer;
        public Vector2 GetLockedAim() => _lockedAim;
        public int GetTotalShots() => _totalShots;
        public Godot.Collections.Dictionary<string, int> GetAttackCounts() => new()
        {
            { "Fan", _attackCounts[0] }, { "Ring", _attackCounts[1] }, { "Charge", _attackCounts[2] }
        };
    }
}
