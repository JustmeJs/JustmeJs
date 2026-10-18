using Godot;
using KleinesSpiel.Scripts.Common;

namespace KleinesSpiel.Scripts.Items
{
    public partial class ShopOffer : Node2D
    {
        public Rooms.RoomDefinition Stock;
        // 0 and 1 are single-use hearts; 2 is a stackable passive upgrade.
        public int Slot;
        private Label _price;
        private Label _hint;
        private Node2D _visual;
        private Player.Player _player;
        public int GetPrice() => Slot < 2 ? Economy.HeartPrice : Economy.PassivePrice;
        public bool IsSold() => Slot < 2 ? Stock.ShopHeartsSold[Slot] : Stock.ShopPassiveSold;

        public override void _Ready()
        {
            _player = GetTree().GetFirstNodeInGroup("player") as Player.Player;
            _visual = new PickupMotion { Name = "Visual", IsHeart = Slot < 2 };
            Texture2D texture = Slot < 2 ? GD.Load<Texture2D>("res://Resources/Art/heart.tres") : ItemCatalog.Get(Stock.ShopItemIndex).Icon;
            _visual.AddChild(new Sprite2D { Name = "Sprite", Texture = texture, Scale = Vector2.One * ((Slot < 2 ? 20f : 36f) / texture.GetWidth()) });
            AddChild(_visual);
            _price = new Label { Name = "Price", Position = new Vector2(-65, 35), Size = new Vector2(130, 30), HorizontalAlignment = HorizontalAlignment.Center };
            AddChild(_price);
            _hint = new Label { Name = "Hint", Position = new Vector2(-95, 66), Size = new Vector2(190, 100),
                AutowrapMode = TextServer.AutowrapMode.WordSmart, HorizontalAlignment = HorizontalAlignment.Center };
            _hint.AddThemeFontSizeOverride("font_size", 14);
            AddChild(_hint);
        }
        public override void _Process(double delta)
        {
            if (!IsInstanceValid(_player)) return;
            bool nearby = _player.GlobalPosition.DistanceTo(GlobalPosition) < 54;
            _visual.Visible = !IsSold();
            _price.Text = IsSold() ? Localization.Text("Sold", "Verkauft")
                : Localization.Format("{0} coins", "{0} Münzen", GetPrice());
            _price.Modulate = !IsSold() && _player.GetCoins() < GetPrice() ? new Color(0.75f, 0.65f, 0.6f) : new Color(1, 0.85f, 0.4f);
            _hint.Visible = nearby && !IsSold();
            _hint.Text = Slot < 2 ? Localization.Text("E: Buy one full heart", "E: Ein ganzes Herz kaufen")
                : Localization.Format("E: Buy {0}", "E: {0} kaufen", ItemCatalog.Get(Stock.ShopItemIndex).GetDisplayName()) + "\n" + ItemCatalog.Get(Stock.ShopItemIndex).GetEffectDescription();
            if (nearby && !IsSold() && Input.IsActionJustPressed("interact")) Buy();
            QueueRedraw();
        }
        private void Buy()
        {
            if (GetTree().Paused || _player.GetCurrentHealthHalfHearts() <= 0) return;
            var hud = GetTree().GetFirstNodeInGroup("hud") as UI.HUD;
            if (Slot < 2 && _player.GetCurrentHealthHalfHearts() >= _player.GetMaxHealthHalfHearts())
            {
                hud?.ShowItemPopup(Localization.Text("Already at full health", "Du hast bereits volle Gesundheit"), "", 2);
                return;
            }
            if (!_player.TrySpendCoins(GetPrice()))
            {
                hud?.ShowItemPopup(Localization.Text("Not enough coins", "Nicht genug Münzen"),
                    Localization.Format("You need {0} coins.", "Du brauchst {0} Münzen.", GetPrice()), 2);
                return;
            }
            if (Slot < 2)
            {
                Stock.ShopHeartsSold[Slot] = true;
                _player.Heal(2);
                hud?.ShowItemPopup(Localization.Text("One full heart", "Ein ganzes Herz"), Localization.Text("Health restored", "Gesundheit wiederhergestellt"), 2);
            }
            else
            {
                Stock.ShopPassiveSold = true;
                var item = ItemCatalog.Get(Stock.ShopItemIndex);
                _player.ApplyItemEffect(item.EffectType, item.EffectValue);
                hud?.ShowItemPopup(item.GetDisplayName(), item.GetEffectDescription());
            }
            Effects.BurstEffect.Spawn(this, GlobalPosition, new Color(1, 0.85f, 0.3f), 22, 0.3f);
        }
        public override void _Draw()
        {
            DrawRect(new Rect2(-24, 15, 48, 14), new Color(0.28f, 0.17f, 0.1f));
            DrawRect(new Rect2(-26, 12, 52, 5), new Color(0.65f, 0.45f, 0.2f));
        }
    }
}
