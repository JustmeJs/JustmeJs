using Godot;
using System.Collections.Generic;

namespace KleinesSpiel.Scripts.Enemies
{
    public static class EnemyAppearance
    {
        private static readonly Dictionary<int, SpriteFrames> Frames = new();
        public static void ClearCache()
        {
            foreach (var frames in Frames.Values) frames.Dispose();
            Frames.Clear();
        }
        public static void Apply(Enemy enemy, bool castle, bool boss, bool zombie = false)
        {
            int index = zombie ? (castle ? 7 : 6) : enemy is EnemySummoner ? (castle ? 5 : 4)
                : !castle ? -1 : enemy is EnemyRanged ? 0 : enemy is EnemyMelee ? 1
                : enemy is CastleBoss || enemy is Boss || boss ? 3 : 2;
            if (index < 0) return;
            if (!Frames.TryGetValue(index, out var frames))
            {
                frames = new SpriteFrames();
                frames.RemoveAnimation("default");
                var atlas = GD.Load<Texture2D>("res://Assets/Sprites/zone_enemies.png");
                foreach (string state in new[] { "idle", "walk", "attack", "hurt", "death", "dash" })
                {
                    frames.AddAnimation(state);
                    frames.SetAnimationSpeed(state, state == "walk" ? 10 : 6);
                    frames.SetAnimationLoop(state, state is "idle" or "walk");
                    for (int f = 0; f < 4; f++)
                        frames.AddFrame(state, new AtlasTexture { Atlas = atlas,
                            Region = new Rect2(index % 4 * 384, index / 4 * 512, 384, 512),
                            Margin = new Rect2(0, state == "walk" ? (f % 2) * 8 : 0, 0, 8) });
                }
                Frames[index] = frames;
            }
            var sprite = enemy.GetNode<AnimatedSprite2D>("Visual/Sprite");
            sprite.SpriteFrames = frames;
            sprite.Scale = Vector2.One * (boss ? 0.16f : zombie ? 0.085f : 0.10f);
            enemy.GetNode<Common.ActorAnimator>("Visual").ProceduralPose = true;
        }
    }
}
