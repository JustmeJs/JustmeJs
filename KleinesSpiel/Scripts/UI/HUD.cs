using Godot;
using KleinesSpiel.Scripts.Common;

namespace KleinesSpiel.Scripts.UI
{
	public partial class HUD : CanvasLayer
	{
		private Label _healthLabel;
        private Label _zoneLabel;
		private Label _multiplierLabel;
		private Label _itemPopupLabel;
		private float _itemPopupTimer;
		private ProgressBar _bossHealthBar;
		private Label _bossHealthBarLabel;
		private Enemies.Enemy _bossReference;
		private Player.Player _player;

		public override void _Ready()
		{
			_healthLabel = GetNodeOrNull<Label>("HealthLabel");
			_multiplierLabel = GetNodeOrNull<Label>("MultiplierLabel");
			_itemPopupLabel = GetNodeOrNull<Label>("ItemPopupLabel");
			_bossHealthBar = GetNodeOrNull<ProgressBar>("BossHealthBar");
			_bossHealthBarLabel = GetNodeOrNull<Label>("BossHealthBarLabel");
			AddToGroup("hud");
            _zoneLabel = new Label { Position = new Vector2(16, 300), Size = new Vector2(235, 90),
                AutowrapMode = TextServer.AutowrapMode.WordSmart };
            AddChild(_zoneLabel);
		}

		public override void _Process(double delta)
		{
			if (_player == null)
			{
				_player = GetTree().GetFirstNodeInGroup("player") as Player.Player;
				if (_player == null)
					return;
			}

			if (_healthLabel != null)
			{
				int current = _player.GetCurrentHealthHalfHearts();
				int max = _player.GetMaxHealthHalfHearts();
				_healthLabel.Text = Localization.Format("Health: {0}/{1}", "Leben: {0}/{1}", current, max);
			}

			if (_multiplierLabel != null)
			{
				float multiplier = _player.GetDamageMultiplier();
				_multiplierLabel.Text = Localization.Format("Multiplier: x{0:0.0}", "Multiplikator: x{0:0.0}", multiplier);
			}

			if (_itemPopupTimer > 0f)
			{
				_itemPopupTimer -= (float)delta;
				if (_itemPopupTimer <= 0f && _itemPopupLabel != null)
				{
					_itemPopupLabel.Visible = false;
				}
			}

			_zoneLabel.Text = Localization.Format("Floor {0}/{1}\n{2}", "Ebene {0}/{1}\n{2}", Rooms.DungeonManager.Instance.GetZoneIndex(), Rooms.DungeonFloor.FloorCount, Rooms.DungeonManager.Instance.GetZoneName());
            UpdateBossHealthBar();
		}

		// Haelt die untere Boss-Lebensleiste (wie in TBOI) synchron mit dem
		// aktuellen Leben des Bosses. Versteckt sich automatisch, sobald der
		// Boss stirbt (das Node wird dann ungueltig, siehe IsInstanceValid).
		private void UpdateBossHealthBar()
		{
			if (_bossReference == null)
				return;

			if (!IsInstanceValid(_bossReference))
			{
				_bossReference = null;
				if (_bossHealthBar != null)
					_bossHealthBar.Visible = false;
				if (_bossHealthBarLabel != null)
					_bossHealthBarLabel.Visible = false;
				return;
			}

			if (_bossHealthBar != null)
			{
				_bossHealthBar.MaxValue = _bossReference.GetMaxHealth();
				_bossHealthBar.Value = _bossReference.GetCurrentHealth();
			}
		}

		// Wird von Room.cs aufgerufen, sobald ein zufaelliger Boss gespawnt wurde.
		public void ShowBossHealthBar(Enemies.Enemy boss, string bossName = "Boss")
		{
			_bossReference = boss;

			if (_bossHealthBar != null)
			{
				_bossHealthBar.Visible = true;
				_bossHealthBar.MaxValue = boss.GetMaxHealth();
				_bossHealthBar.Value = boss.GetCurrentHealth();
			}

			if (_bossHealthBarLabel != null)
			{
				_bossHealthBarLabel.Visible = true;
				_bossHealthBarLabel.Text = bossName;
			}
		}

        public void HideBossHealthBar()
        {
            _bossReference = null;
            if (_bossHealthBar != null) _bossHealthBar.Visible = false;
            if (_bossHealthBarLabel != null) _bossHealthBarLabel.Visible = false;
        }

		// Wird von ItemPickup.cs aufgerufen, wenn der Spieler ein Item einsammelt.
		// Zeigt Name + Effekt fuer ein paar Sekunden an (wie in TBOI).
		public void ShowItemPopup(string itemName, string effectDescription, float durationSeconds = 6f)
		{
			if (_itemPopupLabel == null)
				return;

			_itemPopupLabel.Text = $"{itemName}\n{effectDescription}";
			_itemPopupLabel.Visible = true;
			_itemPopupTimer = durationSeconds;
		}
	}
}
