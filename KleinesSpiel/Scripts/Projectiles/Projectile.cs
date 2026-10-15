using Godot;
using System.Collections.Generic;

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
		private bool _spent;
		private Sprite2D _sprite;
		private Vector2 _spriteScale;
		private float _visualTime;
		private float _trailTimer;
		private readonly List<Vector2> _trail = new();
		private readonly HashSet<ulong> _hitEnemies = new();
		private Color TrailColor => CollisionLayer == 8
			? new Color(0.2f, 0.8f, 1f) : new Color(1, 0.3f, 0.1f);

		public override void _Ready()
		{
			_lifetimeTimer = LifetimeSeconds;
			BodyEntered += OnBodyEntered;
			AreaEntered += OnAreaEntered;
			_sprite = GetNodeOrNull<Sprite2D>("Sprite");
			if (_sprite != null) _spriteScale = _sprite.Scale;
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
			if (_spent) return;
			float dt = (float)delta;
			_visualTime += dt;
			if (_sprite != null)
				_sprite.Scale = _spriteScale * new Vector2(1.1f + Mathf.Sin(_visualTime * 32) * 0.15f,
					0.9f + Mathf.Cos(_visualTime * 32) * 0.12f);
			_trailTimer -= dt;
			if (_trailTimer <= 0)
			{
				_trailTimer = 0.02f;
				_trail.Add(GlobalPosition);
				if (_trail.Count > 6) _trail.RemoveAt(0);
			}
			QueueRedraw();

			Position += _direction * _speed * dt;

			_lifetimeTimer -= dt;
			if (_lifetimeTimer <= 0f)
			{
				Impact();
			}
		}

		public override void _Draw()
		{
			for (int i = 1; i < _trail.Count; i++)
				DrawLine(ToLocal(_trail[i - 1]), ToLocal(_trail[i]),
					new Color(TrailColor, (float)i / _trail.Count * 0.5f), 2);
		}

		private void Impact()
		{
			if (_spent) return;
			_spent = true;
			Effects.BurstEffect.Spawn(this, GlobalPosition, TrailColor, 12, 0.2f);
			QueueFree();
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
			if (_spent) return;
			if (node is Enemies.Enemy enemy)
			{
				if (!_hitEnemies.Add(enemy.GetInstanceId())) return;
				enemy.TakeDamage(_damage);
				Effects.BurstEffect.Spawn(this, GlobalPosition, TrailColor, 9, 0.18f);
				PierceOrFree();
				return;
			}

			if (node is Player.Player player)
			{
				player.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(_damage)));
				Impact();
				return;
			}

			// Wand oder etwas anderes getroffen -> Projektil zerstoeren.
			Impact();
		}

		private void PierceOrFree()
		{
			if (_remainingPierceHits > 0)
			{
				_remainingPierceHits--;
				return;
			}

			Impact();
		}
	}
}
