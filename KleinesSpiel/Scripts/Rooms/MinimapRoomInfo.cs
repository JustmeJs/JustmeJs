using Godot;

namespace KleinesSpiel.Scripts.Rooms
{
	public enum RoomMapType
	{
		Start,
		Normal,
		Treasure,
		Boss,
	}

	// Ein Eintrag pro bekannter Sala in der Minimap. "Visited" unterscheidet
	// zwischen einer Sala, die man schon betreten hat (voll sichtbar), und
	// einer, die man nur von einer Nachbar-Sala aus durch eine Tuer kennt
	// (grau, Typ noch unbekannt) - genau wie in The Binding of Isaac.
	public class MinimapRoomInfo
	{
		public Vector2I GridPosition;
		public RoomMapType Type;
		public bool Visited;
	}
}
