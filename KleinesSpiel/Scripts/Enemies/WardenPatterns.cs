using Godot;
using System.Collections.Generic;

namespace KleinesSpiel.Scripts.Enemies
{
    public static class WardenPatterns
    {
        // Cross / X alternate; the sixth volley is a rarer ring with a navigable gap.
        public static string Name(int volley) => volley % 6 == 5 ? "Ring" : volley % 2 == 1 ? "X" : "Cross";
        public static IEnumerable<Vector2> Directions(int volley, Vector2 aim)
        {
            if (Name(volley) == "Ring")
            {
                float gap = aim.Angle();
                for (int i = 0; i < 20; i++)
                {
                    var direction = Vector2.Right.Rotated(gap + Mathf.Tau * i / 20);
                    if (Mathf.Abs(aim.AngleTo(direction)) >= Mathf.DegToRad(27)) yield return direction;
                }
            }
            else
                for (int i = 0; i < 4; i++)
                    yield return Vector2.Right.Rotated(i * Mathf.Pi / 2 + (volley % 2 == 1 ? Mathf.Pi / 4 : 0));
        }
    }
}
