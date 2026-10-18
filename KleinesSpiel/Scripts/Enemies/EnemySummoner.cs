using Godot;

namespace KleinesSpiel.Scripts.Enemies
{
    // Summoners are harmless themselves; their finite living minion cap prevents flooding.
    public partial class EnemySummoner : Enemy
    {
        private float _summonTimer = 5;
        private Rooms.Room _room;
        public int SummonsCreated { get; private set; }
        public override void _Ready()
        {
            base._Ready();
            ContactDamageHalfHearts = 0;
            for (Node node = GetParent(); node != null; node = node.GetParent())
                if (node is Rooms.Room room) { _room = room; break; }
        }
        protected override void UpdateBehavior(float dt)
        {
            Velocity = Vector2.Zero;
            _summonTimer -= dt;
            if (_summonTimer <= 0 && _room != null && CurrentHealth > 0)
            {
                _summonTimer = 5;
                if (_room.TrySummonZombie(this))
                {
                    SummonsCreated++;
                    Audio.GameAudio.Instance?.PlayEffect("enemy_cast");
                    AnimateAttack(Vector2.Down);
                    Effects.BurstEffect.Spawn(this, GlobalPosition, new Color(0.4f, 1, 0.45f), 32, 0.4f);
                }
            }
        }
    }
}
