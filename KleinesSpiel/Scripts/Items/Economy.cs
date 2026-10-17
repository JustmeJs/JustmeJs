using System;

namespace KleinesSpiel.Scripts.Items
{
    // Project-specific rates: these are balancing choices, not claimed Isaac drop rates.
    public static class Economy
    {
        public const double EnemyCoinChance = 0.35;
        public const int BossCoins = 3;
        public const int HeartPrice = 3;
        public const int PassivePrice = 15;
        public const int WalletLimit = 99;
        private static readonly int[] RepeatableItems = { 0, 1, 2, 3, 5 };
        public static int ShopItem(Random random) => RepeatableItems[random.Next(RepeatableItems.Length)];
        public static int EnemyDrop(Random random) => random.NextDouble() < EnemyCoinChance ? 1 : 0;
    }
}
