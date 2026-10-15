using Godot;

namespace KleinesSpiel.Scripts.Items
{
	public partial class ItemPickup : Area2D
	{
		[Export] public ItemData Data;

		// Wird ausgeloest, wenn der Spieler dieses Item tatsaechlich abholt.
		// Der Raum nutzt das, um das Item als "schon abgeholt"
		// zu markieren, damit beim erneuten Betreten kein neues Item spawnt.
		public event System.Action Collected;

		private bool _collected;

		public override void _Ready()
		{
			BodyEntered += OnBodyEntered;

			var visualRoot = GetNodeOrNull<Node>("Visual");
			var sprite = visualRoot?.GetNodeOrNull<Sprite2D>("Sprite");
			if (sprite != null && Data?.Icon != null)
				sprite.Texture = Data.Icon;

		}

		private void OnBodyEntered(Node2D body)
		{
			if (_collected) return;
			if (Data == null)
				return;

			if (body is Player.Player player)
			{
				_collected = true;
				SetDeferred("monitoring", false);
				GD.Print($"ItemPickup: Spieler hat '{Data.ItemName}' aufgesammelt.");

				player.ApplyItemEffect(Data.EffectType, Data.EffectValue);

				var hud = GetTree().GetFirstNodeInGroup("hud") as UI.HUD;
				hud?.ShowItemPopup(Data.ItemName, Data.EffectDescription);

				Collected?.Invoke();
				var motion = GetNodeOrNull<PickupMotion>("Visual");
				if (motion != null) motion.Collect();
				else QueueFree();
			}
		}
	}
}
