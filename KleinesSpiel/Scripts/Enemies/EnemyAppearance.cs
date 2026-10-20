using Godot;
using System.Collections.Generic;

namespace KleinesSpiel.Scripts.Enemies
{
    public static class EnemyAppearance
    {
        private static readonly Dictionary<string, string> Paths = new()
        {
            ["crypt_imp"] = "res://Resources/Animations/Directions/crypt_imp.tres",
            ["crypt_archer"] = "res://Resources/Animations/Directions/crypt_archer.tres",
            ["crypt_watcher"] = "res://Resources/Animations/Directions/crypt_watcher.tres",
            ["crypt_ogre"] = "res://Resources/Animations/Directions/crypt_ogre.tres",
            ["crypt_guardian"] = "res://Resources/Animations/Directions/crypt_guardian.tres",
            ["crypt_summoner"] = "res://Resources/Animations/Directions/crypt_summoner.tres",
            ["crypt_zombie"] = "res://Resources/Animations/Directions/crypt_zombie.tres",
            ["castle_archer"] = "res://Resources/Animations/Directions/castle_archer.tres",
            ["castle_knight"] = "res://Resources/Animations/Directions/castle_knight.tres",
            ["castle_mage"] = "res://Resources/Animations/Directions/castle_mage.tres",
            ["castle_warden"] = "res://Resources/Animations/Directions/castle_warden.tres",
            ["castle_summoner"] = "res://Resources/Animations/Directions/castle_summoner.tres",
            ["castle_zombie"] = "res://Resources/Animations/Directions/castle_zombie.tres",
        };
        private static readonly Dictionary<string, SpriteFrames> Frames = new();
        public static void ClearCache()
        {
            foreach (var frames in Frames.Values) frames.Dispose();
            Frames.Clear();
        }
        public static void Apply(Enemy enemy, bool castle, bool boss, bool zombie = false)
        {
            string role = zombie ? (castle ? "castle_zombie" : "crypt_zombie")
                : enemy is EnemySummoner ? (castle ? "castle_summoner" : "crypt_summoner")
                : enemy is EnemyRanged ? (castle ? "castle_archer" : "crypt_archer")
                : enemy is EnemyMelee ? (castle ? "castle_knight" : boss ? "crypt_ogre" : "crypt_imp")
                : enemy is CastleBoss || boss ? (castle ? "castle_warden" : "crypt_guardian")
                : castle ? "castle_mage" : "crypt_watcher";
            if (!Frames.TryGetValue(role, out var frames))
            {
                frames = GD.Load<SpriteFrames>(Paths[role]);
                Frames[role] = frames;
            }
            var sprite = enemy.GetNode<AnimatedSprite2D>("Visual/Sprite");
            sprite.SpriteFrames = frames;
            sprite.Scale = Vector2.One * (boss ? 0.32f : enemy is EnemySummoner ? 0.32f : zombie ? 0.16f : 0.18f);
            var animator = enemy.GetNode<Common.ActorAnimator>("Visual");
            animator.MirrorLeft = true;
            animator.HurtFlashOnly = boss;
            animator.RadialAttack = role == "crypt_watcher";
        }
    }
}
