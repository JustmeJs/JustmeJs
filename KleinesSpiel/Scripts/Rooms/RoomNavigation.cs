using Godot;
using System.Collections.Generic;
using System.Linq;

namespace KleinesSpiel.Scripts.Rooms
{
    // One clearance-aware grid per body size. Smoothed look-ahead avoids stair-step
    // movement and recomputing from the nearest cell cannot send actors backwards.
    public sealed class RoomNavigation
    {
        private readonly IReadOnlyList<Rect2> _obstacles;
        private readonly Dictionary<int, AStarGrid2D> _grids = new();
        private static readonly Vector2 Origin = new(288, 64);
        private const int Cells = 29;
        private const int Step = 16;
        public RoomNavigation(int layout) => _obstacles = RoomLayouts.Obstacles(layout);

        public Vector2 Waypoint(Vector2 from, Vector2 target, float radius)
        {
            int clearance = Mathf.CeilToInt(radius) + 3;
            var bounds = new Rect2(Origin + Vector2.One * clearance, Vector2.One * (448 - 2 * clearance));
            target = target.Clamp(bounds.Position, bounds.End);
            if (Clear(from, target, clearance)) return target;
            if (!_grids.TryGetValue(clearance, out var grid))
            {
                grid = new AStarGrid2D
                {
                    Region = new Rect2I(0, 0, Cells, Cells), CellSize = Vector2.One * Step, Offset = Origin,
                    DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles,
                    DefaultComputeHeuristic = AStarGrid2D.Heuristic.Octile,
                    DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Octile
                };
                grid.Update();
                for (int y = 0; y < Cells; y++)
                for (int x = 0; x < Cells; x++)
                {
                    Vector2 point = Origin + new Vector2(x, y) * Step;
                    grid.SetPointSolid(new Vector2I(x, y), !bounds.HasPoint(point)
                        || _obstacles.Any(r => r.Grow(clearance).HasPoint(point)));
                }
                _grids.Add(clearance, grid);
            }
            Vector2I start = Nearest(grid, from), finish = Nearest(grid, target);
            var path = grid.GetPointPath(start, finish);
            for (int i = path.Length - 1; i >= 0; i--)
                if (Clear(from, path[i], clearance)) return path[i];
            return from;
        }

        private static Vector2I Nearest(AStarGrid2D grid, Vector2 point)
        {
            var nearest = new Vector2I(1, 1);
            float best = float.MaxValue;
            for (int y = 0; y < Cells; y++)
            for (int x = 0; x < Cells; x++)
            {
                var cell = new Vector2I(x, y);
                if (grid.IsPointSolid(cell)) continue;
                float distance = (Origin + new Vector2(x, y) * Step).DistanceSquaredTo(point);
                if (distance < best) { best = distance; nearest = cell; }
            }
            return nearest;
        }

        private bool Clear(Vector2 from, Vector2 to, float clearance)
        {
            foreach (var obstacle in _obstacles)
            {
                var box = obstacle.Grow(clearance);
                // Recover actors already touching a pillar: the safety margin
                // must not make their current position an inescapable solid cell.
                if (box.HasPoint(from)) box = obstacle.Grow(clearance - 3.1f);
                if (Crosses(from, to, box)) return false;
            }
            return true;
        }

        private static bool Crosses(Vector2 from, Vector2 to, Rect2 box)
        {
            float low = 0, high = 1;
            Vector2 delta = to - from;
            for (int axis = 0; axis < 2; axis++)
            {
                if (Mathf.Abs(delta[axis]) < 0.0001f)
                {
                    if (from[axis] < box.Position[axis] || from[axis] > box.End[axis]) return false;
                }
                else
                {
                    float a = (box.Position[axis] - from[axis]) / delta[axis];
                    float b = (box.End[axis] - from[axis]) / delta[axis];
                    low = Mathf.Max(low, Mathf.Min(a, b));
                    high = Mathf.Min(high, Mathf.Max(a, b));
                    if (low > high) return false;
                }
            }
            return true;
        }
    }
}
