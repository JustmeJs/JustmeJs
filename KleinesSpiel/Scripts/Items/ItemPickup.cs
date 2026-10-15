using Godot;

namespace KleinesSpiel.Scripts.Items
{
	public partial class ItemPickup : Area2D
	{
		[Export] public ItemData Data;

		// Wird ausgeloest, wenn der Spieler dieses Item tatsaechlich abholt.
		// TreasureItemSpawner nutzt das, um den Raum als "schon abgeholt"
		// zu markieren, damit beim erneuten Betreten kein neues Item spawnt.
		public event System.Action Collected;

		public override void _Ready()
		{
			BodyEntered += OnBodyEntered;

			var visualRoot = GetNodeOrNull<Node>("Visual");
			var sprite = visualRoot?.GetNodeOrNull<Sprite2D>("Sprite");
			if (sprite != null && Data?.Icon != null)
				sprite.Texture = Data.Icon;
			Common.VisualHelper.UseSpriteIfAssigned(visualRoot);

			// Kleine Auf-und-Ab-Bewegung, damit das Item auf dem Boden
			// nicht so leicht zu uebersehen ist (wie das Glitzern in TBOI).
			// Bewegt "Visual" selbst, nicht nur den Placeholder, damit es
			// funktioniert egal ob gerade der Placeholder oder das eigene
			// Sprite sichtbar ist.
			var visual = GetNodeOrNull<CanvasItem>("Visual");
			if (visual != null)
			{
				var tween = GetTree().CreateTween();
				tween.SetLoops();
				tween.TweenProperty(visual, "position:y", -6f, 0.5f).SetTrans(Tween.TransitionType.Sine);
				tween.TweenProperty(visual, "position:y", 0f, 0.5f).SetTrans(Tween.TransitionType.Sine);
			}

			CallDeferred(nameof(PrintDebugInfo));
		}

		private void PrintDebugInfo()
		{
			GD.Print($"ItemPickup DEBUG: Pfad={GetPath()}, Visible={Visible}, GlobalPosition={GlobalPosition}, InTree={IsInsideTree()}");
		}

		private void OnBodyEntered(Node2D body)
		{
			if (Data == null)
				return;

			if (body is Player.Player player)
			{
				GD.Print($"ItemPickup: Spieler hat '{Data.ItemName}' aufgesammelt.");

				player.ApplyItemEffect(Data.EffectType, Data.EffectValue);

				var hud = GetTree().GetFirstNodeInGroup("hud") as UI.HUD;
				hud?.ShowItemPopup(Data.ItemName, Data.EffectDescription);

				Collected?.Invoke();
				QueueFree();
			}
		}
	}
}
