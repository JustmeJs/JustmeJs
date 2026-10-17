using System;
using Godot;

namespace KleinesSpiel.Scripts.Enemies
{
	// Basisklasse fuer alle Gegner. EnemyMelee, EnemyRanged, EnemyFourWay
	// und Boss erben davon und ueberschreiben nur UpdateBehavior().
	public partial class Enemy : CharacterBody2D
	{
		[ExportGroup("Leben")]
		[Export] public float MaxHealth = 4f;

		[ExportGroup("Bewegung")]
		[Export] public float MoveSpeed = 100f;

		[ExportGroup("Kontaktschaden")]
		[Export] public int ContactDamageHalfHearts = 1;
		[Export] public float ContactDamageCooldownSeconds = 0.6f;
		// Both actors separate after contact; chasing pauses long enough to dodge.
		[Export] public float ContactKnockbackForce = 220f;
		[Export] public float ContactRecoilSpeed = 150f;
		[Export] public float ContactRecoilSeconds = 0.22f;
		[Export] public float ContactRecoverySeconds = 0.28f;

		[ExportGroup("Herz-Drop")]
		// Wahrscheinlichkeit (0-1), dass beim Tod dieses Gegners ein
		// Herz zum Aufsammeln liegen bleibt. Bosse setzen das im
		// Editor typischerweise auf 1.0 (garantierter Drop).
		[Export(PropertyHint.Range, "0,1,0.01")] public float HeartDropChance = 0.12f;

		// Wird ausgeloest, wenn dieser Gegner stirbt. Das Room-Skript
		// hoert auf dieses Event, um zu wissen, wann der Raum leer ist.
		public event Action<Enemy> Defeated;

		protected float CurrentHealth;
		protected Node2D PlayerReference; // wird bei Bedarf gesucht (fuer Verfolgung/Zielen)

		private float _contactDamageTimer;
		private float _contactRecoilTimer;
		private float _contactRecoveryTimer;
		private Vector2 _contactRecoilVelocity;
		private Node2D _visual;
		private Common.ActorAnimator _animator;

		public override void _Ready()
		{
			CurrentHealth = MaxHealth;
			_visual = GetNodeOrNull<Node2D>("Visual");
			_animator = _visual as Common.ActorAnimator;
			AddToGroup("enemies");
		}

		public override void _PhysicsProcess(double delta)
		{
			float dt = (float)delta;

			if (_contactDamageTimer > 0f)
				_contactDamageTimer -= dt;

			if (PlayerReference == null)
			{
				PlayerReference = GetTree().GetFirstNodeInGroup("player") as Node2D;
			}

			if (_contactRecoilTimer > 0f)
			{
				_contactRecoilTimer -= dt;
				Velocity = _contactRecoilVelocity;
			}
			else if (_contactRecoveryTimer > 0f)
			{
				_contactRecoveryTimer -= dt;
				Velocity = Vector2.Zero;
			}
			else
			{
				UpdateBehavior(dt);
			}
			MoveAndSlide();
			CheckContactDamage();
			_animator?.SetMotion(Velocity, Velocity);
		}

		protected void AnimateAttack(Vector2 direction)
		{
			_animator?.PlayAttack(direction);
			Effects.BurstEffect.Spawn(this, GlobalPosition + direction * 16,
				new Color(1, 0.45f, 0.2f), 11, 0.18f);
		}

		// Wird von den Unterklassen ueberschrieben (Verfolgen, Abstand halten, etc.)
		protected virtual void UpdateBehavior(float dt)
		{
		}

		protected Vector2 GetDirectionToPlayer()
		{
			if (PlayerReference == null)
				return Vector2.Zero;

			return (PlayerReference.GlobalPosition - GlobalPosition).Normalized();
		}

		protected float GetDistanceToPlayer()
		{
			if (PlayerReference == null)
				return float.MaxValue;

			return GlobalPosition.DistanceTo(PlayerReference.GlobalPosition);
		}

		private void CheckContactDamage()
		{
			if (_contactDamageTimer > 0f || _contactRecoilTimer > 0f || _contactRecoveryTimer > 0f)
				return;

			for (int i = 0; i < GetSlideCollisionCount(); i++)
			{
				var collision = GetSlideCollision(i);
				if (collision.GetCollider() is Player.Player player)
				{
					Vector2 directionToPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
					if (directionToPlayer.IsZeroApprox()) directionToPlayer = -collision.GetNormal();
					AnimateAttack(directionToPlayer);
					_contactDamageTimer = ContactDamageCooldownSeconds;
					BeginContactRecovery(-directionToPlayer);

					// Dash and hurt immunity also prevent knockback. Otherwise a crowd
					// can keep resetting the player's forced movement without a hit.
					if (!player.IsInvincible() && player.GetCurrentHealthHalfHearts() > 0)
					{
						int healthBefore = player.GetCurrentHealthHalfHearts();
						player.TakeDamage(ContactDamageHalfHearts);
						if (player.GetCurrentHealthHalfHearts() > 0 && player.GetCurrentHealthHalfHearts() < healthBefore)
							player.ApplyKnockback(directionToPlayer, ContactKnockbackForce);
					}
					break;
				}
			}
		}

		private void BeginContactRecovery(Vector2 awayFromPlayer)
		{
			// Prefer stepping directly back. Against a wall, choose a clear diagonal
			// or sidestep; MoveAndSlide still handles actual movement and collisions.
			Vector2 retreat = awayFromPlayer;
			float distance = Mathf.Max(ContactRecoilSpeed * ContactRecoilSeconds, 1f);
			foreach (float angle in new[] { 0f, Mathf.Pi / 4f, -Mathf.Pi / 4f, Mathf.Pi / 2f, -Mathf.Pi / 2f })
			{
				Vector2 candidate = awayFromPlayer.Rotated(angle);
				if (!TestMove(GlobalTransform, candidate * distance))
				{
					retreat = candidate;
					break;
				}
			}
			_contactRecoilVelocity = retreat * ContactRecoilSpeed;
			_contactRecoilTimer = ContactRecoilSeconds;
			_contactRecoveryTimer = ContactRecoverySeconds;
		}

		public void TakeDamage(float amount)
		{
			if (CurrentHealth <= 0f)
				return;

			CurrentHealth -= amount;
			FlashHurt();

			// Der Spieler bekommt den Schaden-Multiplikator-Bonus fuer JEDEN
			// verbundenen Treffer, nicht erst wenn der Gegner stirbt.
			var player = GetTree().GetFirstNodeInGroup("player") as Player.Player;
			player?.OnEnemyHitWithoutDamageTaken();

			if (CurrentHealth <= 0f)
			{
				Die();
			}
		}

		private void FlashHurt()
		{
			_animator?.PlayHurt();
		}

		protected virtual void Die()
		{
			SetPhysicsProcess(false);
			Velocity = Vector2.Zero;
			CollisionLayer = 0;
			CollisionMask = 0;
			RemoveFromGroup("enemies");
			_animator?.PlayDeath();
			TrySpawnHeartDrop();
			Defeated?.Invoke(this);
			// The actor is harmless immediately; only its death visual remains.
			var cleanup = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
			cleanup.TweenInterval(0.55f);
			cleanup.TweenCallback(Callable.From(QueueFree));
		}

		// Wird von der HUD (Boss-Lebensbalken) benutzt, um den aktuellen
		// Lebensstand abzulesen, ohne CurrentHealth komplett public zu machen.
		public float GetCurrentHealth() => CurrentHealth;
		public float GetMaxHealth() => MaxHealth;

		private static PackedScene _heartPickupScene;

		// Wuerfelt anhand von HeartDropChance, ob ein Herz liegen bleibt,
		// und spawnt es an der Todesposition dieses Gegners. Nutzt dieselbe
		// CallDeferred-Absicherung: der Elternknoten
		// koennte in seltenen Faellen gerade selbst am Aufbauen sein.
		private void TrySpawnHeartDrop()
		{
			if (HeartDropChance <= 0f)
				return;

			var rng = new RandomNumberGenerator();
			rng.Randomize();
			if (rng.Randf() > HeartDropChance)
				return;

			_heartPickupScene ??= GD.Load<PackedScene>("res://Scenes/Items/HeartPickup.tscn");
			if (_heartPickupScene == null)
				return;

			var parent = GetParent();
			if (parent == null)
				return;

			var heartInstance = _heartPickupScene.Instantiate<Node2D>();
			Vector2 spawnPosition = GlobalPosition;

			parent.CallDeferred(Node.MethodName.AddChild, heartInstance);
			heartInstance.CallDeferred(Node2D.MethodName.SetGlobalPosition, spawnPosition);
		}

		// -----------------------------------------------------------------
		// Wird von Room.cs benutzt, wenn ein Raum bereits als geleert gilt
		// und dieser Gegner deshalb gar nicht "leben" soll. QueueFree()
		// allein reicht nicht: das Node bleibt bis zum Ende des Frames aktiv
		// und wuerde in diesem letzten Frame noch schiessen oder Kontakt-
		// schaden austeilen (Geisterschuss-Bug). Deshalb zuerst komplett
		// stilllegen und dann erst freigeben.
		// -----------------------------------------------------------------
		public void DisableAndFree()
		{
			SetPhysicsProcess(false);
			SetProcess(false);
			CollisionLayer = 0;
			CollisionMask = 0;
			Visible = false;
			QueueFree();
		}
	}
}
