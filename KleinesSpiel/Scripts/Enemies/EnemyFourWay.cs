using Godot;

namespace KleinesSpiel.Scripts.Enemies
{
	// Bewegt sich langsam/zufaellig und schiesst regelmaessig Projektile
	// in alle 4 Richtungen gleichzeitig (oben, unten, links, rechts).
	public partial class EnemyFourWay : Enemy
	{
		public bool BossPatterns;
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
					projectile.Launch(direction, ProjectileSpeed * ProjectileSpeedMultiplier, ProjectileDamage);
				}
			}
		}
	}
}
