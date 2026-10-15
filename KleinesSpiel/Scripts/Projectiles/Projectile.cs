using Godot;

namespace KleinesSpiel.Scripts.Projectiles
{
	// Wird sowohl vom Spieler als auch von Gegnern benutzt.
	// "Launch" setzt Richtung, Geschwindigkeit und Schaden fest.
	public partial class Projectile : Area2D
	{
		[Export] public float LifetimeSeconds = 3f; // zerstoert sich nach dieser Zeit von selbst
		[Export] public bool Piercing = false; // true = durchdringt einen Gegner (Item-Effekt)

		private Vector2 _direction = Vector2.Zero;
		private float _speed;
		private float _damage;
		private float _lifetimeTimer;
		private int _remainingPierceHits = 0;

		public override void _Ready()
		{
			_lifetimeTimer = LifetimeSeconds;
			BodyEntered += OnBodyEntered;
			AreaEntered += OnAreaEntered;
		}

		public void Launch(Vector2 direction, float speed, float damage)
		{
			_direction = direction.Normalized();
			_speed = speed;
			_damage = damage;

			if (Piercing)
			{
				_remainingPierceHits = 1;
			}

			// Projektil in Bewegungsrichtung drehen (nuetzlich fuer laengliche Sprites).
			Rotation = _direction.Angle();
		}

		public override void _PhysicsProcess(double delta)
		{
			float dt = (float)delta;

			Position += _direction * _speed * dt;

			_lifetimeTimer -= dt;
			if (_lifetimeTimer <= 0f)
			{
				QueueFree();
			}
		}

		private void OnBodyEntered(Node2D body)
		{
			HandleHit(body);
		}

		private void OnAreaEntered(Area2D area)
		{
			HandleHit(area);
		}

		private void HandleHit(Node node)
		{
			if (node is Enemies.Enemy enemy)
			{
				enemy.TakeDamage(_damage);
				PierceOrFree();
				return;
			}

			if (node is Player.Player player)
			{
				player.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(_damage)));
				QueueFree();
				return;
			}

			// Wand oder etwas anderes getroffen -> Projektil zerstoeren.
			QueueFree();
		}

		private void PierceOrFree()
		{
			if (_remainingPierceHits > 0)
			{
				_remainingPierceHits--;
				return;
			}

			QueueFree();
		}
	}
}
