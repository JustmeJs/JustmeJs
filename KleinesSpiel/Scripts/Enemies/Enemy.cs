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
		// Wie stark der Spieler bei einem Treffer weggestossen wird - ohne das
		// bleibt er (v.a. bei Nahkampf-Gegnern) am Gegner "kleben", weil beide
		// jeden Frame direkt aufeinander zulaufen. Der Stoss schafft sofort
		// wieder Abstand, damit man sich wegbewegen kann.
		[Export] public float ContactKnockbackForce = 220f;

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
		private Node2D _visual;
		private AnimatedSprite2D _animatedSprite;

		// Ab welcher Geschwindigkeit gilt der Gegner als "in Bewegung"
		// (unterhalb davon wird die Lauf-Animation angehalten).
		private const float AnimationMoveThreshold = 5f;

		public override void _Ready()
		{
			CurrentHealth = MaxHealth;
			_visual = GetNodeOrNull<Node2D>("Visual");
			_animatedSprite = _visual?.GetNodeOrNull<AnimatedSprite2D>("Sprite");
			Common.VisualHelper.UseSpriteIfAssigned(_visual);
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

			UpdateBehavior(dt);
			MoveAndSlide();
			CheckContactDamage();
			UpdateDirectionalAnimation();
		}

		// Waehlt anhand von Velocity die passende Lauf-Animation
		// (walk_down / walk_up / walk_left) und spiegelt sie fuer "rechts"
		// (FlipH), statt eine eigene "walk_right"-Animation zu brauchen.
		// Setzt jede Unterklasse automatisch, solange ihr SpriteFrames-Asset
		// die drei genannten Animationsnamen enthaelt.
		private void UpdateDirectionalAnimation()
		{
			if (_animatedSprite == null || _animatedSprite.SpriteFrames == null)
				return;

			if (Velocity.Length() < AnimationMoveThreshold)
			{
				_animatedSprite.Stop();
				return;
			}

			string animationName;
			bool flipH = false;

			if (Mathf.Abs(Velocity.X) > Mathf.Abs(Velocity.Y))
			{
				animationName = "walk_left";
				flipH = Velocity.X > 0f; // "rechts" = gespiegeltes "walk_left"
			}
			else
			{
				animationName = Velocity.Y < 0f ? "walk_up" : "walk_down";
			}

			if (!_animatedSprite.SpriteFrames.HasAnimation(animationName))
				return;

			_animatedSprite.FlipH = flipH;

			if (_animatedSprite.Animation != animationName || !_animatedSprite.IsPlaying())
			{
				_animatedSprite.Play(animationName);
			}
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
			if (_contactDamageTimer > 0f)
				return;

			for (int i = 0; i < GetSlideCollisionCount(); i++)
			{
				var collision = GetSlideCollision(i);
				if (collision.GetCollider() is Player.Player player)
				{
					player.TakeDamage(ContactDamageHalfHearts);
					_contactDamageTimer = ContactDamageCooldownSeconds;

					Vector2 knockbackDirection = (player.GlobalPosition - GlobalPosition).Normalized();
					player.ApplyKnockback(knockbackDirection, ContactKnockbackForce);
				}
			}
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
			if (_visual == null)
				return;

			var tween = GetTree().CreateTween();
			tween.TweenProperty(_visual, "modulate", new Color(2f, 2f, 2f, 1f), 0.03f);
			tween.TweenProperty(_visual, "modulate", new Color(1f, 1f, 1f, 1f), 0.12f);
		}

		protected virtual void Die()
		{
			TrySpawnHeartDrop();
			Defeated?.Invoke(this);
			QueueFree();
		}

		// Wird von der HUD (Boss-Lebensbalken) benutzt, um den aktuellen
		// Lebensstand abzulesen, ohne CurrentHealth komplett public zu machen.
		public float GetCurrentHealth() => CurrentHealth;
		public float GetMaxHealth() => MaxHealth;

		private static PackedScene _heartPickupScene;

		// Wuerfelt anhand von HeartDropChance, ob ein Herz liegen bleibt,
		// und spawnt es an der Todesposition dieses Gegners. Nutzt dieselbe
		// CallDeferred-Absicherung wie TreasureItemSpawner: der Elternknoten
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
