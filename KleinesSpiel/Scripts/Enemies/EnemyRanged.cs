using Godot;

namespace KleinesSpiel.Scripts.Enemies
{
	// Haelt Abstand zum Spieler und schiesst Projektile in seine Richtung.
	// Bewegt sich weg, wenn der Spieler zu nah ist, und naeher, wenn er zu
	// weit weg ist, um innerhalb der PreferredDistance zu bleiben.
	public partial class EnemyRanged : Enemy
	{
		[ExportGroup("Fernkampf")]
		[Export] public PackedScene ProjectileScene;
		[Export] public float PreferredDistance = 220f;
		[Export] public float FireCooldownSeconds = 1.4f;
		[Export] public float ProjectileSpeed = 220f;
		[Export] public float ProjectileDamage = 1f;

		private float _fireCooldownTimer;

		protected override void UpdateBehavior(float dt)
		{
			float distance = GetDistanceToPlayer();
			Vector2 directionToPlayer = GetDirectionToPlayer();

			if (distance < PreferredDistance - 20f)
			{
				// Zu nah -> wegbewegen
				Velocity = -directionToPlayer * MoveSpeed;
			}
			else if (distance > PreferredDistance + 20f)
			{
				// Zu weit weg -> naeher kommen
				Velocity = directionToPlayer * MoveSpeed;
			}
			else
			{
				Velocity = Vector2.Zero;
			}

			_fireCooldownTimer -= dt;
			if (CanShoot && _fireCooldownTimer <= 0f && PlayerReference != null)
			{
				Shoot(directionToPlayer);
				_fireCooldownTimer = FireCooldownSeconds * ShotIntervalMultiplier;
			}
		}

		private void Shoot(Vector2 direction)
		{
			if (ProjectileScene == null)
				return;

			AnimateAttack(direction);

			var projectileInstance = ProjectileScene.Instantiate<Node2D>();
			GetTree().CurrentScene.AddChild(projectileInstance);
			projectileInstance.GlobalPosition = GlobalPosition;

			if (projectileInstance is Projectiles.Projectile projectile)
			{
				projectile.IsArrow = true;
                projectile.Launch(direction, ProjectileSpeed * ProjectileSpeedMultiplier, ProjectileDamage);
			}
		}
	}
}
