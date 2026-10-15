using Godot;

namespace KleinesSpiel.Scripts.UI
{
    // Visited rooms and discovered neighbors of the generated floor.
	public partial class Minimap : Control
	{
		private const float CellSize = 16f;
		private const float CellGap = 5f;
		private const float Padding = 8f;

		public override void _Ready()
		{
			CustomMinimumSize = new Vector2(150, 110);
			MouseFilter = MouseFilterEnum.Ignore;

			var manager = Rooms.DungeonManager.Instance;
			if (manager != null)
			{
				manager.MapChanged += OnMapChanged;
			}
		}

		public override void _ExitTree()
		{
			var manager = Rooms.DungeonManager.Instance;
			if (manager != null)
			{
				manager.MapChanged -= OnMapChanged;
			}
		}

		private void OnMapChanged()
		{
			QueueRedraw();
		}

		public override void _Draw()
		{
			var manager = Rooms.DungeonManager.Instance;
			if (manager == null)
				return;

			var entries = manager.GetMapEntries();
			if (entries.Count == 0)
				return;

			string currentPath = manager.GetCurrentRoomId();

			// Begrenzung des bisher bekannten Gitters ermitteln, damit alles
			// unabhaengig von negativen Koordinaten (z.B. eine Sala links vom
			// Start) sauber innerhalb des Panels gezeichnet wird.
			int minX = int.MaxValue, maxX = int.MinValue;
			int minY = int.MaxValue, maxY = int.MinValue;
			foreach (var info in entries.Values)
			{
				minX = Mathf.Min(minX, info.GridPosition.X);
				maxX = Mathf.Max(maxX, info.GridPosition.X);
				minY = Mathf.Min(minY, info.GridPosition.Y);
				maxY = Mathf.Max(maxY, info.GridPosition.Y);
			}

			float step = CellSize + CellGap;
			float panelWidth = (maxX - minX + 1) * step - CellGap + Padding * 2f;
			float panelHeight = (maxY - minY + 1) * step - CellGap + Padding * 2f;

			DrawRect(new Rect2(Vector2.Zero, new Vector2(panelWidth, panelHeight)), new Color(0f, 0f, 0f, 0.45f));

			foreach (var kvp in entries)
			{
				var info = kvp.Value;
				Vector2 cellOrigin = new Vector2(
					Padding + (info.GridPosition.X - minX) * step,
					Padding + (info.GridPosition.Y - minY) * step
				);
				var rect = new Rect2(cellOrigin, new Vector2(CellSize, CellSize));

				DrawRect(rect, GetColorFor(info));

				if (kvp.Key == currentPath)
				{
					DrawRect(rect, Colors.White, filled: false, width: 2f);
				}
			}
		}

		private static Color GetColorFor(Rooms.MinimapRoomInfo info)
		{
			if (!info.Visited)
				return new Color(0.35f, 0.35f, 0.38f, 0.9f);

			return info.Type switch
			{
				Rooms.RoomMapType.Start => new Color(0.3f, 0.6f, 0.95f, 1f),
				Rooms.RoomMapType.Treasure => new Color(1f, 0.85f, 0.2f, 1f),
				Rooms.RoomMapType.Boss => new Color(0.85f, 0.15f, 0.15f, 1f),
				_ => new Color(0.8f, 0.8f, 0.85f, 1f),
			};
		}
	}
}
