using Godot;
using System.Collections.Generic;

namespace KleinesSpiel.Scripts.Rooms
{
    public static class RoomLayouts
    {
        public static IReadOnlyList<Rect2> Obstacles(int layout) => layout switch
        {
            1 => new[] { new Rect2(392, 168, 48, 48), new Rect2(584, 168, 48, 48),
                         new Rect2(392, 360, 48, 48), new Rect2(584, 360, 48, 48) },
            2 => new[] { new Rect2(384, 248, 64, 80), new Rect2(576, 248, 64, 80) },
            3 => new[] { new Rect2(392, 216, 48, 48), new Rect2(584, 312, 48, 48) },
            4 => new[] { new Rect2(360, 184, 48, 96), new Rect2(616, 296, 48, 96) },
            5 => new[] { new Rect2(456, 200, 40, 40), new Rect2(528, 336, 40, 40),
                         new Rect2(392, 268, 40, 40), new Rect2(592, 268, 40, 40) },
            _ => System.Array.Empty<Rect2>()
        };
    }
}
