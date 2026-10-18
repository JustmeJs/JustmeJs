using Godot;

namespace KleinesSpiel.Scripts.Player
{
	// Richtung, in die der Spieler zuletzt geschaut / geschossen hat.
	// Wird auch fuer den Dash benutzt.
	public enum Direction4
	{
		Down,
		Up,
		Left,
		Right
	}

	public partial class Player : CharacterBody2D
	{
		// ---------------------------------------------------------
		// Werte, die im Godot-Editor im Inspector angepasst werden koennen.
		// So muss man fuer's Balancing keinen Code aendern, nur Zahlen im Editor.
		// ---------------------------------------------------------

		[ExportGroup("Bewegung")]
		[Export] public float MoveSpeed = 150f; // px/Sekunde, wie in Isaac

		[ExportGroup("Leben")]
		[Export] public int MaxHealthHalfHearts = 8; // 4 volle Herzen = 8 halbe Herzen
		[Export] public float HurtInvincibilitySeconds = 11f / 60f; // 11 Frames bei 60 FPS

		[ExportGroup("Schiessen")]
		[Export] public PackedScene ProjectileScene; // Szene des Spieler-Projektils
		[Export] public float FireCooldownSeconds = 0.4f;
		[Export] public float ProjectileSpeed = 300f;
		[Export] public float BaseDamage = 1f;

		[ExportGroup("Zauberstab: Projektil-Ursprung")]
		[Export] public Vector2 CastOffsetDown = new(-0.56f, 13.23f);
		[Export] public Vector2 CastOffsetUp = new(-5.49f, -16.86f);
		[Export] public Vector2 CastOffsetLeft = new(-17.61f, 0.96f);
		[Export] public Vector2 CastOffsetRight = new(19.4f, -0.04f);

		[ExportGroup("Dash")]
		[Export] public float DashDistance = 48f; // kurzer Ruck nach vorne
		[Export] public float DashDurationSeconds = 0.15f; // wie lange der Dash dauert
		[Export] public float DashInvincibilitySeconds = 0.25f;

		[ExportGroup("Schaden-Multiplikator")]
		[Export] public float DamageMultiplierStep = 0.1f; // +0.1x pro Treffer (nicht pro Kill)

		[ExportGroup("Rueckstoss bei Treffer")]
		// Wie lange die Bewegungseingabe (WASD) kurz ausgesetzt und stattdessen
		// der Rueckstoss erzwungen wird. Schiessen/Zielen bleibt weiter moeglich.
		[Export] public float KnockbackDurationSeconds = 0.18f;

		// ---------------------------------------------------------
		// Interner Zustand (wird waehrend des Spiels veraendert)
		// ---------------------------------------------------------

		private int _coins;
        public int GetCoins() => _coins;
        public int AddCoins(int amount)
        {
            if (amount <= 0) return 0;
            int added = Mathf.Min(amount, Items.Economy.WalletLimit - _coins);
            _coins += added;
            return added;
        }
        public bool TrySpendCoins(int price)
        {
            if (price <= 0 || _coins < price) return false;
            _coins -= price;
            return true;
        }

        private int _currentHealthHalfHearts;
		private Direction4 _facingDirection = Direction4.Down;

		private float _fireCooldownTimer;
		private float _hurtInvincibilityTimer;

		private bool _isDashing;
		private float _dashTimer;
		private float _dashInvincibilityTimer;
		private Vector2 _dashDirectionVector;

		private bool _dashUsedThisRoom;

		private Vector2 _knockbackVelocity = Vector2.Zero;
		private float _knockbackTimer;

		private float _blinkTimer;
		private const float BlinkIntervalSeconds = 0.05f; // wie schnell er waehrend der i-Frames flackert

		private float _damageMultiplier = 1f;
		private bool _hasPiercingUpgrade = false;

		// Optionaler Kindknoten fuer die Anzeige (Sprite2D). Kann null sein,
		// falls noch kein Sprite im Editor zugewiesen wurde.
		private Node2D _visual;
		private Common.ActorAnimator _animator;
		private bool _dead;

		public override void _Ready()
		{
			MotionMode = MotionModeEnum.Floating;
            _currentHealthHalfHearts = MaxHealthHalfHearts;
			_visual = GetNodeOrNull<Node2D>("Visual");
			_animator = _visual as Common.ActorAnimator;
			AddToGroup("player");
		}

		public override void _PhysicsProcess(double delta)
		{
			float dt = (float)delta;

			UpdateTimers(dt);
			UpdateDashBlink(dt);

			if (_knockbackTimer > 0f)
			{
				// Kurzer erzwungener Rueckstoss nach einem Treffer: die WASD-
				// Bewegung wird fuer diesen Moment ausser Kraft gesetzt, damit
				// man wirklich auf Abstand zum Gegner kommt. Zielen/Schiessen
				// bleibt trotzdem moeglich.
				_knockbackTimer -= dt;
				Velocity = _knockbackVelocity;
				ProcessAiming();
				ProcessShooting(dt);
			}
			else if (_isDashing)
			{
				ProcessDash(dt);
			}
			else
			{
				ProcessMovement(dt);
				ProcessAiming();
				ProcessShooting(dt);
				ProcessDashInput();
			}

			MoveAndSlide();
			_animator?.SetMotion(Velocity, IsAimingHeld() ? DirectionToVector(_facingDirection) : Velocity);
		}

		// -----------------------------------------------------------------
		// Bewegung (WASD)
		// -----------------------------------------------------------------

		private void ProcessMovement(float dt)
		{
			Vector2 inputVector = Input.GetVector("move_left", "move_right", "move_up", "move_down");
			Velocity = inputVector * MoveSpeed;
		}

		// -----------------------------------------------------------------
		// Zielrichtung (Pfeiltasten) - bestimmt Schuss- und Dash-Richtung
		// -----------------------------------------------------------------

		private void ProcessAiming()
		{
			if (Input.IsActionPressed("aim_up"))
				_facingDirection = Direction4.Up;
			else if (Input.IsActionPressed("aim_down"))
				_facingDirection = Direction4.Down;
			else if (Input.IsActionPressed("aim_left"))
				_facingDirection = Direction4.Left;
			else if (Input.IsActionPressed("aim_right"))
				_facingDirection = Direction4.Right;

			// ActorAnimator selects the matching directional staff animation.
		}

		private bool IsAimingHeld()
		{
			return Input.IsActionPressed("aim_up")
				|| Input.IsActionPressed("aim_down")
				|| Input.IsActionPressed("aim_left")
				|| Input.IsActionPressed("aim_right");
		}

		private Vector2 DirectionToVector(Direction4 direction)
		{
			return direction switch
			{
				Direction4.Up => new Vector2(0, -1),
				Direction4.Down => new Vector2(0, 1),
				Direction4.Left => new Vector2(-1, 0),
				Direction4.Right => new Vector2(1, 0),
				_ => Vector2.Zero
			};
		}

		// -----------------------------------------------------------------
		// Schiessen (nur wenn eine Pfeiltaste gehalten wird)
		// -----------------------------------------------------------------

		private void ProcessShooting(float dt)
		{
			if (_fireCooldownTimer > 0f)
			{
				_fireCooldownTimer -= dt;
			}

			if (IsAimingHeld() && _fireCooldownTimer <= 0f)
			{
				Shoot();
				_fireCooldownTimer = FireCooldownSeconds;
			}
		}

		private void Shoot()
		{
			if (ProjectileScene == null)
			{
				// Noch keine Projektil-Szene zugewiesen -> nichts tun.
				// (Im Editor im Inspector "ProjectileScene" setzen.)
				return;
			}

			Vector2 direction = DirectionToVector(_facingDirection);
			Vector2 origin = GetCastOrigin();
			var projectileInstance = ProjectileScene.Instantiate<Node2D>();
			GetParent().AddChild(projectileInstance);
			projectileInstance.GlobalPosition = origin;

			if (projectileInstance is Projectiles.Projectile projectile)
			{
				_animator?.PlayAttack(direction);
				Effects.BurstEffect.Spawn(this, origin, new Color(0.3f, 0.9f, 1), 9, 0.14f);
				float finalDamage = BaseDamage * _damageMultiplier;
				if (_hasPiercingUpgrade)
				{
					projectile.Piercing = true;
				}
				projectile.Launch(direction, ProjectileSpeed, finalDamage);
			}
		}

		public Vector2 GetCastOrigin()
		{
			Vector2 offset = _facingDirection switch
			{
				Direction4.Up => CastOffsetUp,
				Direction4.Left => CastOffsetLeft,
				Direction4.Right => CastOffsetRight,
				_ => CastOffsetDown
			};
			return ToGlobal(offset);
		}

		// -----------------------------------------------------------------
		// Dash (Leertaste)
		// -----------------------------------------------------------------

		private void ProcessDashInput()
		{
			if (_dashUsedThisRoom)
				return;

			if (Input.IsActionJustPressed("dash"))
			{
				StartDash();
			}
		}

		private void StartDash()
		{
			// Dash-Richtung: die aktuelle Bewegungsrichtung, falls der Spieler
			// sich bewegt, sonst die Blickrichtung (Schussrichtung).
			Vector2 direction = Velocity.Normalized();
			if (direction == Vector2.Zero)
			{
				direction = DirectionToVector(_facingDirection);
			}

			_isDashing = true;
			_dashUsedThisRoom = true;
			_dashTimer = DashDurationSeconds;
			_dashInvincibilityTimer = DashInvincibilitySeconds;
			_dashDirectionVector = direction;
			_animator?.PlayDash(DashDurationSeconds);
		}

		private void ProcessDash(float dt)
		{
			_dashTimer -= dt;

			// Wichtig: Geschwindigkeit statt direkter Positionsaenderung,
			// damit MoveAndSlide() weiterhin gegen Waende kollidiert.
			float dashSpeed = DashDistance / DashDurationSeconds;
			Velocity = _dashDirectionVector * dashSpeed;

			if (_dashTimer <= 0f)
			{
				_isDashing = false;
			}
		}

		// Wird von Enemy.cs bei Kontaktschaden aufgerufen, um sofort wieder
		// Abstand zwischen Spieler und Gegner zu schaffen.
		public void ApplyKnockback(Vector2 direction, float force)
		{
			if (direction == Vector2.Zero)
				return;

			_knockbackVelocity = direction.Normalized() * force;
			_knockbackTimer = KnockbackDurationSeconds;
		}

		public bool IsInvincible()
		{
			return _hurtInvincibilityTimer > 0f || _dashInvincibilityTimer > 0f;
		}

		// -----------------------------------------------------------------
		// Timer aktualisieren (i-frames nach Treffer, i-frames vom Dash)
		// -----------------------------------------------------------------

		private void UpdateTimers(float dt)
		{
			if (_hurtInvincibilityTimer > 0f)
				_hurtInvincibilityTimer -= dt;

			if (_dashInvincibilityTimer > 0f)
				_dashInvincibilityTimer -= dt;
		}

		// Laesst den Spieler schnell auftauchen/verschwinden, solange die
		// Dash-Unverwundbarkeit aktiv ist - so sieht man genau, wie lange
		// man in diesem Moment unverwundbar ist.
		private void UpdateDashBlink(float dt)
		{
			if (_visual == null)
				return;

			if (_dashInvincibilityTimer > 0f)
			{
				_blinkTimer -= dt;
				if (_blinkTimer <= 0f)
				{
					_blinkTimer = BlinkIntervalSeconds;
					_visual.Visible = !_visual.Visible;
				}
			}
			else if (!_visual.Visible)
			{
				_visual.Visible = true;
				_blinkTimer = 0f;
			}
		}

		// -----------------------------------------------------------------
		// Schaden erhalten
		// -----------------------------------------------------------------

		public void TakeDamage(int halfHeartsAmount)
		{
			if (_dead || IsInvincible())
				return;

			_currentHealthHalfHearts -= halfHeartsAmount;
			_hurtInvincibilityTimer = HurtInvincibilitySeconds;
			_animator?.PlayHurt();
            Audio.GameAudio.Instance?.PlayEffect("hurt");

			// Treffer zurueckgesetzt den Schaden-Multiplikator
			ResetDamageMultiplier();

			if (_currentHealthHalfHearts <= 0)
			{
				Die();
			}
		}

		private void Die()
		{
			if (_dead) return;
			_dead = true;
			SetPhysicsProcess(false);
			CollisionLayer = 0;
			CollisionMask = 0;
			_animator?.PlayDeath();
            Audio.GameAudio.Instance?.OnRunEnded();
            Audio.GameAudio.Instance?.PlayEffect("death");
			GetTree().Paused = true;

			var gameOverScene = GD.Load<PackedScene>("res://Scenes/UI/GameOverScreen.tscn");
			var instance = gameOverScene.Instantiate();
			GetTree().Root.AddChild(instance);
		}

		// -----------------------------------------------------------------
		// Schaden-Multiplikator
		// -----------------------------------------------------------------

		public void OnEnemyHitWithoutDamageTaken()
		{
			_damageMultiplier += DamageMultiplierStep;
			GD.Print($"Schaden-Multiplikator gestiegen: x{_damageMultiplier:0.00}");
		}

		public void ResetDamageMultiplier()
		{
			_damageMultiplier = 1f;
		}

		public float GetDamageMultiplier()
		{
			return _damageMultiplier;
		}

		// -----------------------------------------------------------------
		// Wird vom Room-Skript aufgerufen, wenn der Spieler einen neuen Raum betritt.
		// Setzt den Dash und den Schaden-Multiplikator zurueck.
		// -----------------------------------------------------------------

		public void OnEnteredNewRoom()
		{
			_dashUsedThisRoom = false;
            _isDashing = false;
            _dashTimer = 0;
            _knockbackTimer = 0;
            _knockbackVelocity = Vector2.Zero;
            Velocity = Vector2.Zero;
			ResetDamageMultiplier();
		}

		public int GetCurrentHealthHalfHearts() => _currentHealthHalfHearts;
		public int GetMaxHealthHalfHearts() => MaxHealthHalfHearts;

		// -----------------------------------------------------------------
		// Leben zurueckgeben (Herz-Pickups). Heilt nie ueber das Maximum
		// hinaus - bei voller Leben passiert einfach nichts.
		// -----------------------------------------------------------------

		public void Heal(int halfHeartsAmount)
		{
			if (halfHeartsAmount <= 0)
				return;

			_currentHealthHalfHearts = Mathf.Min(_currentHealthHalfHearts + halfHeartsAmount, MaxHealthHalfHearts);
		}

		// -----------------------------------------------------------------
		// Effekt eines passiven Items anwenden
		// -----------------------------------------------------------------

		public void ApplyItemEffect(Items.ItemEffectType effectType, float value)
		{
			switch (effectType)
			{
				case Items.ItemEffectType.ExtraHeart:
					int extraHalfHearts = Mathf.RoundToInt(value);
					MaxHealthHalfHearts += extraHalfHearts;
					_currentHealthHalfHearts += extraHalfHearts;
					break;

				case Items.ItemEffectType.MoveSpeedPercent:
					MoveSpeed *= 1f + value / 100f;
					break;

				case Items.ItemEffectType.FireRatePercent:
					// Hoehere Feuerrate = kuerzerer Cooldown.
					FireCooldownSeconds *= 1f - value / 100f;
					FireCooldownSeconds = Mathf.Max(FireCooldownSeconds, 0.05f);
					break;

				case Items.ItemEffectType.DamagePercent:
					BaseDamage *= 1f + value / 100f;
					break;

				case Items.ItemEffectType.ProjectileSpeedPercent:
					ProjectileSpeed *= 1f + value / 100f;
					break;

				case Items.ItemEffectType.Piercing:
					_hasPiercingUpgrade = true;
					break;
			}
		}
	}
}
