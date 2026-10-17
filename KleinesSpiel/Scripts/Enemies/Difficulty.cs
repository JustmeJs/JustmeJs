using Godot;

namespace KleinesSpiel.Scripts.Enemies
{
    public static class Difficulty
    {
        public const float EntryGraceSeconds = 1.5f;
        public static float Movement(int floor) => 0.80f + 0.06f * (Mathf.Clamp(floor, 1, 6) - 1);
        public static float ShotInterval(int floor) => 1.45f - 0.13f * (Mathf.Clamp(floor, 1, 6) - 1);
        public static float ProjectileSpeed(int floor) => 0.75f + 0.06f * (Mathf.Clamp(floor, 1, 6) - 1);
        public static void Apply(Enemy enemy, int floor, bool boss)
        {
            if (enemy is not CastleBoss)
                enemy.MaxHealth *= 1 + (floor - 1) * (boss ? 0.18f : 0.14f);
            enemy.MoveSpeed = Mathf.Min(130, enemy.MoveSpeed * Movement(floor));
            enemy.ShotIntervalMultiplier = ShotInterval(floor);
            enemy.ProjectileSpeedMultiplier = ProjectileSpeed(floor);
        }
    }
}
