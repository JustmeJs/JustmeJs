using Godot;

namespace KleinesSpiel.Scripts.Common
{
	// Kleine Helferklasse fuer alle Entitaeten (Player, Enemy, ItemPickup):
	// Jede "Visual"-Node darf zwei Kinder haben:
	//   "Sprite"      -> Sprite2D (Standbild) ODER AnimatedSprite2D (Animation)
	//   "Placeholder" -> der alte Farb-Platzhalter (ColorRect/Polygon2D)
	// Sobald "Sprite" eine Textur bzw. SpriteFrames zugewiesen bekommt, wird
	// der Placeholder automatisch ausgeblendet. Ohne zugewiesenes Asset
	// bleibt der Placeholder sichtbar, damit man Assets nach und nach
	// reinziehen kann, ohne dass zwischendurch etwas unsichtbar auf dem
	// Feld steht.
	public static class VisualHelper
	{
		public static void UseSpriteIfAssigned(Node visualRoot)
		{
			if (visualRoot == null)
				return;

			var placeholder = visualRoot.GetNodeOrNull<CanvasItem>("Placeholder");
			bool hasArt = false;

			var animatedSprite = visualRoot.GetNodeOrNull<AnimatedSprite2D>("Sprite");
			if (animatedSprite != null && animatedSprite.SpriteFrames != null
				&& animatedSprite.SpriteFrames.GetAnimationNames().Length > 0)
			{
				hasArt = true;
			}

			var staticSprite = visualRoot.GetNodeOrNull<Sprite2D>("Sprite");
			if (staticSprite != null && staticSprite.Texture != null)
			{
				hasArt = true;
			}

			if (hasArt && placeholder != null)
			{
				placeholder.Visible = false;
			}
		}
	}
}
