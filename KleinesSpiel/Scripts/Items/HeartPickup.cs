using Godot;

namespace KleinesSpiel.Scripts.Items
{
	// Ein aufsammelbares Herz, das verlorene Lebenspunkte zurueckgibt.
	// Wird von Gegnern zufaellig fallen gelassen (siehe Enemy.HeartDropChance).
	// Kann auch bei voller Lebensanzeige aufgesammelt werden - dann passiert
	// einfach nichts, es verschwindet trotzdem.
	public partial class HeartPickup : Area2D
	{
		// 2 halbe Herzen = 1 volles Herz.
		[Export] public int HealHalfHearts = 2;

		private bool _collected;

		public override void _Ready()
		{
			BodyEntered += OnBodyEntered;

			var visualRoot = GetNodeOrNull<Node>("Visual");
			Common.VisualHelper.UseSpriteIfAssigned(visualRoot);

		}

		private void OnBodyEntered(Node2D body)
		{
			if (_collected) return;
			if (body is Player.Player player)
			{
				_collected = true;
				SetDeferred("monitoring", false);
				player.Heal(HealHalfHearts);
				var motion = GetNodeOrNull<PickupMotion>("Visual");
				if (motion != null) motion.Collect();
				else QueueFree();
			}
		}
	}
}
