using Godot;
using KleinesSpiel.Scripts.Common;

namespace KleinesSpiel.Scripts.Items
{
	public enum ItemEffectType
	{
		ExtraHeart,
		MoveSpeedPercent,
		FireRatePercent,
		DamagePercent,
		Piercing,
		ProjectileSpeedPercent
	}

	// Ein "Datenpaket" fuer ein passives Item. Jedes Item wird als eigene
	// .tres-Ressource gespeichert - neue Items hinzufuegen heisst also nur
	// eine neue .tres-Datei erstellen, kein neuer Code noetig.
	[GlobalClass]
	public partial class ItemData : Resource
	{
		[Export] public string ItemName = "";
		[Export] public string EffectDescription = "";
		[Export] public string GermanItemName = "";
		[Export] public string GermanEffectDescription = "";
		[Export] public ItemEffectType EffectType = ItemEffectType.DamagePercent;
		[Export] public float EffectValue = 0f;
		[Export] public Texture2D Icon;

		public string GetDisplayName() => Localization.Text(ItemName,
			string.IsNullOrEmpty(GermanItemName) ? ItemName : GermanItemName);
		public string GetEffectDescription() => Localization.Text(EffectDescription,
			string.IsNullOrEmpty(GermanEffectDescription) ? EffectDescription : GermanEffectDescription);
	}
}
