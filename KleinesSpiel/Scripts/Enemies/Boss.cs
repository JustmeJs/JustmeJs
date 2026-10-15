using Godot;

namespace KleinesSpiel.Scripts.Enemies
{
	// Der Boss ist eine groessere, staerkere Version der normalen Gegner.
	// Er wechselt zwischen zwei einfachen Phasen:
	// 1) Den Spieler kurz verfolgen (wie EnemyMelee)
	// 2) Stehen bleiben und in 4 Richtungen schiessen (wie EnemyFourWay)
	// MoveSpeed sollte im Editor unter 150 px/s bleiben (nie schneller als der Spieler).
	public partial class Boss : Enemy
	{
		[ExportGroup("Boss-Angriffe")]
		[Export] public PackedScene ProjectileScene;
		[Export] public float ChasePhaseSeconds = 2.5f;
		[Export] public float ShootPhaseSeconds = 1.5f;
		[Export] public float ProjectileSpeed = 220f;
		[Export] public float ProjectileDamage = 1f;
		[Export] public float ShotsPerShootPhase = 3f;

		private enum Phase
		{
			Chasing,
			Shooting
		}

		private Phase _currentPhase = Phase.Chasing;
		private float _phaseTimer;
		private float _shotTimer;
		private float _timeBetweenShots;

		public override void _Ready()
		{
			base._Ready();
			_phaseTimer = ChasePhaseSeconds;
			_timeBetweenShots = ShootPhaseSeconds / Mathf.Max(ShotsPerShootPhase, 1f);
		}

		protected override void UpdateBehavior(float dt)
		{
			_phaseTimer -= dt;

			if (_currentPhase == Phase.Chasing)
			{
				Velocity = GetDirectionToPlayer() * MoveSpeed;

				if (_phaseTimer <= 0f)
				{
					_currentPhase = Phase.Shooting;
					_phaseTimer = ShootPhaseSeconds;
					_shotTimer = 0f;
				}
			}
			else // Shooting
			{
				Velocity = Vector2.Zero;

				_shotTimer -= dt;
				if (_shotTimer <= 0f)
				{
					ShootFourWays();
					_shotTimer = _timeBetweenShots;
				}

				if (_phaseTimer <= 0f)
				{
					_currentPhase = Phase.Chasing;
					_phaseTimer = ChasePhaseSeconds;
				}
			}
		}

		private void ShootFourWays()
		{
			if (ProjectileScene == null)
				return;

			AnimateAttack(GetDirectionToPlayer());

			Vector2[] directions =
			{
				new Vector2(0, -1),
				new Vector2(0, 1),
				new Vector2(-1, 0),
				new Vector2(1, 0)
			};

			foreach (var direction in directions)
			{
				var projectileInstance = ProjectileScene.Instantiate<Node2D>();
				GetTree().CurrentScene.AddChild(projectileInstance);
				projectileInstance.GlobalPosition = GlobalPosition;

				if (projectileInstance is Projectiles.Projectile projectile)
				{
					projectile.Launch(direction, ProjectileSpeed, ProjectileDamage);
				}
			}
		}
	}
}
