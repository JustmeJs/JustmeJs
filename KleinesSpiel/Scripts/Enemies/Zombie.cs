using Godot;
namespace KleinesSpiel.Scripts.Enemies
{
    public partial class Zombie : EnemyMelee
    {
        private readonly RandomNumberGenerator _voiceRandom = new();
        private float _voiceTimer;
        public override void _Ready()
        {
            base._Ready();
            _voiceRandom.Randomize();
            _voiceTimer = _voiceRandom.RandfRange(0.5f, 2f);
        }
        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            if (CurrentHealth <= 0) return;
            _voiceTimer -= (float)delta;
            if (_voiceTimer <= 0)
            {
                Audio.GameAudio.Instance?.PlayEffect("zombie");
                _voiceTimer = _voiceRandom.RandfRange(4f, 7f);
            }
        }
    }
}
