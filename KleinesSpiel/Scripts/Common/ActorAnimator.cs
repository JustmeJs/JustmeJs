using Godot;

namespace KleinesSpiel.Scripts.Common
{
    // Only Visual moves/scales: collision bodies and gameplay timing stay independent.
    public partial class ActorAnimator : Node2D
    {
        [Export] public bool Floating;
        public bool ProceduralPose;
        private float _deathTime;
        [Export] public Color Accent = new Color(0.3f, 0.85f, 1f);
        private AnimatedSprite2D _sprite;
        private Vector2 _spriteScale;
        private Vector2 _motion;
        private Vector2 _facing = Vector2.Down;
        private float _time;
        private float _attack;
        private float _hurt;
        private float _dash;
        private float _trail;
        private bool _dead;
        private bool _directional;
        private string _state = "idle";
        public string GetAnimationState() => _state;

        public override void _Ready()
        {
            _sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite");
            if (_sprite == null) return;
            _spriteScale = _sprite.Scale;
            _directional = _sprite.SpriteFrames.HasAnimation("idle_down");
            _sprite.Play(ResolveAnimation("idle"));
        }

        private string ResolveAnimation(string state)
        {
            string direction = Mathf.Abs(_facing.X) > Mathf.Abs(_facing.Y)
                ? (_facing.X < 0 ? "left" : "right") : (_facing.Y < 0 ? "up" : "down");
            string directionalName = $"{state}_{direction}";
            return _directional && _sprite.SpriteFrames.HasAnimation(directionalName)
                ? directionalName : state;
        }

        public void SetMotion(Vector2 velocity, Vector2 facing)
        {
            _motion = velocity;
            // Kiting archers can move away while still firing toward the player.
            if (!_dead && _attack <= 0 && facing.LengthSquared() > 0.01f) _facing = facing;
        }

        public void PlayAttack(Vector2 direction)
        {
            if (_dead) return;
            _attack = 0.24f;
            if (direction != Vector2.Zero) _facing = direction;
            if (_sprite != null && _hurt <= 0f && _dash <= 0f)
            {
                _sprite.Play(ResolveAnimation("attack"));
                _sprite.SetFrameAndProgress(0, 0);
            }
        }

        public void PlayHurt()
        {
            if (_dead) return;
            _hurt = 0.17f;
            Effects.BurstEffect.Spawn(this, GlobalPosition, new Color(1, 0.45f, 0.3f), 12, 0.22f);
        }

        public void PlayDash(float duration)
        {
            if (!_dead) _dash = duration;
        }

        public void PlayDeath()
        {
            if (_dead) return;
            _dead = true;
            _state = "death";
            Visible = true;
            ProcessMode = ProcessModeEnum.Always;
            _sprite?.Play(ResolveAnimation("death"));
            var effect = Effects.BurstEffect.Spawn(this, GlobalPosition, Accent, 28, 0.5f);
            if (effect != null) effect.ProcessMode = ProcessModeEnum.Always;
        }

        public override void _Process(double delta)
        {
            if (_sprite == null) return;
            float dt = (float)delta;
            _time += dt;
            _attack = Mathf.Max(0, _attack - dt);
            _hurt = Mathf.Max(0, _hurt - dt);
            _dash = Mathf.Max(0, _dash - dt);
            string state = _dead ? "death" : _hurt > 0 ? "hurt" : _dash > 0 ? "dash"
                : _attack > 0 ? "attack" : _motion.LengthSquared() > 25 ? "walk" : "idle";
            string animation = ResolveAnimation(state);
            if (_state != state || _sprite.Animation.ToString() != animation)
            {
                _state = state;
                _sprite.Play(animation);
            }
            if (_directional) _sprite.FlipH = false;
            else if (Mathf.Abs(_facing.X) > 0.1f) _sprite.FlipH = _facing.X < 0;
            float bob = _dead ? 0 : Floating ? Mathf.Sin(_time * 4) * 2
                : state == "walk" ? -Mathf.Abs(Mathf.Sin(_time * 14)) * 1.5f
                : Mathf.Sin(_time * 3) * 0.4f;
            _sprite.Position = new Vector2(0, bob);
            _sprite.Rotation = _dead ? 0 : state == "walk" ? Mathf.Sin(_time * 14) * 0.035f
                : state == "hurt" ? Mathf.Sin(_time * 70) * 0.08f : 0;
            float stretch = state == "dash" ? 0.15f : state == "attack" ? -0.06f : 0;
            _sprite.Scale = _spriteScale * new Vector2(1 + stretch, 1 - stretch);
            _sprite.SelfModulate = state == "hurt" ? new Color(2, 1.3f, 1.3f)
                : state == "dash" ? new Color(0.55f, 1.3f, 1.6f) : Colors.White;
            if (ProceduralPose)
            {
                if (_dead)
                {
                    _deathTime += dt;
                    _sprite.Rotation = _deathTime * 2;
                    _sprite.Scale *= Mathf.Max(0.1f, 1 - _deathTime);
                    _sprite.Modulate = new Color(1, 1, 1, Mathf.Max(0, 1 - _deathTime * 2));
                }
                else if (state == "attack")
                {
                    _sprite.Position += _facing.Normalized() * Mathf.Sin(_attack / 0.24f * Mathf.Pi) * 5;
                    _sprite.Rotation = _facing.X * 0.12f;
                }
            }
            if (state == "dash")
            {
                _trail -= dt;
                if (_trail <= 0)
                {
                    _trail = 0.035f;
                    Effects.BurstEffect.Ghost(this, _sprite, Accent);
                }
            }
        }
    }
}
