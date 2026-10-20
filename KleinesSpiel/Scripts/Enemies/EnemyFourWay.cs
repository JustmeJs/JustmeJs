using Godot;

namespace KleinesSpiel.Scripts.Enemies
{
	// Bewegt sich langsam/zufaellig und schiesst regelmaessig Projektile
	// in alle 4 Richtungen gleichzeitig (oben, unten, links, rechts).
	public partial class EnemyFourWay : Enemy
	{
		public bool BossPatterns;
        [Export] public float ProjectileRange = 240f;
        private float _radialFlash;
        [Export] public ulong AttackSeed;
        private readonly RandomNumberGenerator _attackRandom = new();
        private string _lastPattern = "Cross";
        public string GetLastPattern() => _lastPattern;

        [ExportGroup("Vierfach-Schuss")]
		[Export] public PackedScene ProjectileScene;
		[Export] public float FireCooldownSeconds = 1.8f;
		[Export] public float ProjectileSpeed = 200f;
		[Export] public float ProjectileDamage = 1f;
		[Export] public float WanderSpeedFactor = 0.5f; // langsamer, zufaelliges Wandern

		private float _fireCooldownTimer;
		private Vector2 _wanderDirection = Vector2.Zero;
		private float _wanderChangeTimer;
		private readonly RandomNumberGenerator _rng = new();

		public override void _Ready()
		{
			base._Ready();
            if (AttackSeed == 0) _attackRandom.Randomize(); else _attackRandom.Seed = AttackSeed;
			_rng.Randomize();
		}

		protected override void UpdateBehavior(float dt)
		{
            _radialFlash = Mathf.Max(0, _radialFlash - dt);
            QueueRedraw();
			_wanderChangeTimer -= dt;
			if (_wanderChangeTimer <= 0f)
			{
				_wanderDirection = new Vector2(
					_rng.RandfRange(-1f, 1f),
					_rng.RandfRange(-1f, 1f)
				).Normalized();
				_wanderChangeTimer = _rng.RandfRange(1f, 2.5f);
			}

			Velocity = _wanderDirection * MoveSpeed * WanderSpeedFactor;

			_fireCooldownTimer -= dt;
			if (CanShoot && _fireCooldownTimer <= 0f)
			{
				ShootFourWays();
				_fireCooldownTimer = FireCooldownSeconds * ShotIntervalMultiplier;
			}
		}

		private void ShootFourWays()
		{
			if (ProjectileScene == null)
				return;

			AnimateAttack(GetDirectionToPlayer());
            if (GetNode<Common.ActorAnimator>("Visual").RadialAttack) _radialFlash = 0.24f;

            int pattern = BossPatterns ? WardenPatterns.Choose(_attackRandom) : 0;
            _lastPattern = WardenPatterns.Name(pattern);
            var directions = WardenPatterns.Directions(pattern, GetDirectionToPlayer());

			foreach (var direction in directions)
			{
				var projectileInstance = ProjectileScene.Instantiate<Node2D>();
				GetTree().CurrentScene.AddChild(projectileInstance);
				projectileInstance.GlobalPosition = GlobalPosition;

				if (projectileInstance is Projectiles.Projectile projectile)
				{
                    if (!BossPatterns) projectile.MaxTravelDistance = ProjectileRange;
					projectile.Launch(direction, ProjectileSpeed * ProjectileSpeedMultiplier, ProjectileDamage);
				}
			}
		}
        public override void _Draw()
        {
            if (_radialFlash <= 0 || CurrentHealth <= 0) return;
            float progress = 1 - _radialFlash / 0.24f;
            var color = new Color(0.85f, 0.5f, 1f, 1 - progress);
            DrawArc(Vector2.Zero, 16 + progress * 15, 0, Mathf.Tau, 24, color, 2);
            foreach (var direction in new[] { Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right })
                DrawLine(direction * 12, direction * (25 + progress * 12), color, 3);
        }

	}
}
