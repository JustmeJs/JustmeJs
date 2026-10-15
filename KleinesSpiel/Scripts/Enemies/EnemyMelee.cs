using Godot;

namespace KleinesSpiel.Scripts.Enemies
{
	// Verfolgt den Spieler direkt und macht beim Beruehren Kontaktschaden.
	// MoveSpeed sollte im Editor etwas niedriger als die Spielergeschwindigkeit
	// (150 px/s) eingestellt werden, z. B. 110-120 px/s.
	public partial class EnemyMelee : Enemy
	{
		protected override void UpdateBehavior(float dt)
		{
			Vector2 direction = GetDirectionToPlayer();
			Velocity = direction * MoveSpeed;
		}
	}
}
