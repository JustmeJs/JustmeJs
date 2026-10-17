using Godot;

namespace KleinesSpiel.Scripts.Items
{
    public static class ItemCatalog
    {
        public static readonly string[] Paths =
        {
            "res://Resources/Items/Item01_ExtraHeart.tres",
            "res://Resources/Items/Item02_MoveSpeed.tres",
            "res://Resources/Items/Item03_FireRate.tres",
            "res://Resources/Items/Item04_Damage.tres",
            "res://Resources/Items/Item05_Piercing.tres",
            "res://Resources/Items/Item06_ArcaneLens.tres"
        };
        public static ItemData Get(int index) => GD.Load<ItemData>(Paths[index]);
    }
}
