using System;
namespace KleinesSpiel.Scripts.Audio
{
    public static class RunMusic
    {
        public static string[] Create(int seed)
        {
            var random = new Random(seed ^ 0x4d757369);
            var crypt = new[] { "crypt_rooms", "crypt_ossuary", "crypt_depths" };
            var castle = new[] { "castle_rooms", "castle_gallery", "castle_watch" };
            Rooms.DungeonFloor.Shuffle(crypt, random);
            Rooms.DungeonFloor.Shuffle(castle, random);
            return new[] { crypt[0], crypt[1], crypt[2], castle[0], castle[1], castle[2] };
        }
    }
}
