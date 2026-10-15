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

		public override void _Ready()
		{
			BodyEntered += OnBodyEntered;

			var visualRoot = GetNodeOrNull<Node>("Visual");
			Common.VisualHelper.UseSpriteIfAssigned(visualRoot);

			// Gleiches sanftes Auf-und-Ab wie bei ItemPickup, damit man es
			// zwischen den Gegnerleichen nicht uebersieht.
			var visual = GetNodeOrNull<CanvasItem>("Visual");
			if (visual != null)
			{
				var tween = GetTree().CreateTween();
				tween.SetLoops();
				tween.TweenProperty(visual, "position:y", -5f, 0.45f).SetTrans(Tween.TransitionType.Sine);
				tween.TweenProperty(visual, "position:y", 0f, 0.45f).SetTrans(Tween.TransitionType.Sine);
			}
		}

		private void OnBodyEntered(Node2D body)
		{
			if (body is Player.Player player)
			{
				player.Heal(HealHalfHearts);
				QueueFree();
			}
		}
	}
}
