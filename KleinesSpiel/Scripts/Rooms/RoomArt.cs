using Godot;

namespace KleinesSpiel.Scripts.Rooms
{
    // Visual geometry follows the existing collision shapes, including door gaps.
    [Tool]
    public partial class RoomArt : Node2D
    {
        [Export] public Color StoneTint = Colors.White;
        [Export] public bool Castle;
        public int Design;
        [Export] public RoomMapType RoomKind;
        private Texture2D _wall;
        private float _time;

        public override void _Process(double delta)
        {
            if (Engine.IsEditorHint()) return;
            _time += (float)delta;
            QueueRedraw();
        }

        public override void _Ready()
        {
            _wall = GD.Load<Texture2D>("res://Assets/Sprites/Tiles/stone_wall.svg");
            TextureFilter = TextureFilterEnum.Nearest;
            TextureRepeat = TextureRepeatEnum.Enabled;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_wall == null) return;
            if (Castle) DrawCastleFloor(); else DrawCryptFloor();
            foreach (string group in new[] { "Walls", "Obstacles" })
            {
                var bodies = GetParent().GetNodeOrNull<Node2D>(group);
                if (bodies == null) continue;
                foreach (var child in bodies.GetChildren())
                {
                    if (child is not CollisionShape2D collider || collider.Shape is not RectangleShape2D rectangle) continue;
                    DrawSetTransform(bodies.Position + collider.Position, collider.Rotation, collider.Scale);
                    var rect = new Rect2(-rectangle.Size / 2, rectangle.Size);
                    DrawTextureRect(_wall, rect, true, StoneTint);
                    if (group == "Obstacles")
                    {
                        if (!Castle) DrawCryptObstacle(rect);
                        if (Castle && Design is 2 or 4)
                        {
                            DrawRect(rect.Grow(-3), new Color(0.25f, 0.13f, 0.07f));
                            for (float y = rect.Position.Y + 9; y < rect.End.Y - 4; y += 12)
                                DrawLine(new Vector2(rect.Position.X+5,y), new Vector2(rect.End.X-5,y), new Color(0.65f, 0.43f, 0.22f), 3);
                        }
                        DrawRect(rect, new Color(0.04f, 0.04f, 0.09f), false, 3);
                        DrawLine(rect.Position + Vector2.One * 3, rect.Position + new Vector2(rect.Size.X - 3, 3), StoneTint * 0.65f, 2);
                    }
                }
            }
            DrawSetTransform(Vector2.Zero);
            if (RoomKind is RoomMapType.Treasure or RoomMapType.Boss)
            {
                Color rune = RoomKind == RoomMapType.Treasure ? new Color(1, 0.8f, 0.3f, 0.28f)
                    : new Color(0.7f, 0.3f, 1, 0.24f);
                DrawArc(new Vector2(512,288), RoomKind == RoomMapType.Treasure ? 48 : 100,
                    _time * 0.12f, _time * 0.12f + Mathf.Tau, 48, rune, 2);
                if (RoomKind == RoomMapType.Treasure)
                    DrawRect(new Rect2(496,272,32,32), new Color(0.75f,0.6f,0.3f,0.35f));
            }
            if (RoomKind == RoomMapType.Shop)
            {
                DrawRect(new Rect2(320, 240, 384, 125), new Color(0.08f, 0.23f, 0.17f, 0.9f));
                DrawRect(new Rect2(326, 246, 372, 113), new Color(0.75f, 0.57f, 0.23f), false, 2);
                DrawCircle(new Vector2(512, 100), 15, new Color(0.95f, 0.7f, 0.15f));
                DrawArc(new Vector2(512, 100), 11, 0, Mathf.Tau, 24, new Color(0.45f, 0.27f, 0.07f), 2);
                DrawLine(new Vector2(512, 93), new Vector2(512, 107), new Color(1, 0.94f, 0.6f), 3);
            }
            if (Castle)
            {
                DrawBanner(new Vector2(402, 73));
                DrawBanner(new Vector2(622, 73));
                DrawWindow(new Vector2(282, 160));
                DrawWindow(new Vector2(742, 160));
                DrawWindow(new Vector2(282, 390));
                DrawWindow(new Vector2(742, 390));
            }
            DrawTorch(new Vector2(352, 67), 0);
            DrawTorch(new Vector2(672, 67), 1.7f);
        }

        private void DrawCryptFloor()
        {
            var hues = new[] { new Color(.13f,.15f,.2f), new Color(.18f,.16f,.14f),
                new Color(.09f,.17f,.16f), new Color(.14f,.10f,.2f), new Color(.20f,.13f,.13f), new Color(.12f,.14f,.15f) };
            Color hue = hues[Mathf.Clamp(Design,0,5)];
            for (int x = 0; x < 10; x++) for (int y = 0; y < 10; y++)
            {
                var at = new Vector2(272+x*48,48+y*48);
                DrawRect(new Rect2(at+Vector2.One,Vector2.One*46), (x+y)%2==0 ? hue : hue*.85f);
                if ((x*3+y*7+Design)%5 == 0)
                    DrawPolyline(new[] {at+new Vector2(8,6),at+new Vector2(20,19),at+new Vector2(15,28),at+new Vector2(30,43)}, new Color(.03f,.04f,.06f,.8f), 2);
            }
            var center = new Vector2(512,288);
            Color dust = new Color(.38f,.4f,.37f,.45f);
            switch (Design)
            {
                case 0: // An old processional aisle, with worn stone crosses.
                    DrawRect(new Rect2(486,64,52,448), new Color(.23f,.22f,.2f,.65f));
                    for (int y=120;y<500;y+=85)
                    { DrawLine(new Vector2(503,y),new Vector2(521,y),dust,2); DrawLine(new Vector2(512,y-10),new Vector2(512,y+16),dust,2); }
                    break;
                case 1: // Four burial plots; collider art becomes carved tombs.
                    foreach (var p in new[] {new Vector2(416,192),new Vector2(608,192),new Vector2(416,384),new Vector2(608,384)})
                    { DrawRect(new Rect2(p-new Vector2(38,42),new Vector2(76,84)),dust,false,2); DrawCircle(p+new Vector2(0,36),3,new Color(.7f,.55f,.25f)); }
                    break;
                case 2: // Damp crypt, shallow green channels painted onto traversable floor.
                    for(int y=100;y<500;y+=96)
                    { DrawRect(new Rect2(300,y,424,14),new Color(.08f,.28f,.24f,.6f)); DrawLine(new Vector2(306,y+4),new Vector2(718,y+4),new Color(.3f,.5f,.38f,.3f),2); }
                    break;
                case 3: // Astral burial chamber, broken ritual rings.
                    for(int r=80;r<=160;r+=40)
                        DrawArc(center,r,.3f,r%80==0 ? 5.7f : 4.9f,48,new Color(.5f,.32f,.68f,.35f),2);
                    for(int i=0;i<8;i++)
                    { var p=center+Vector2.Right.Rotated(i*Mathf.Tau/8)*140; DrawLine(p-Vector2.Down*7,p+Vector2.Down*7,dust,2); }
                    break;
                case 4: // Ossuary rows and a narrow crimson processional rug.
                    DrawRect(new Rect2(493,80,38,416),new Color(.29f,.08f,.1f,.7f));
                    for(int y=110;y<500;y+=65) foreach(int x in new[]{314,710})
                    { DrawCircle(new Vector2(x,y),6,dust); DrawCircle(new Vector2(x-2,y-1),1.5f,Colors.Black); DrawCircle(new Vector2(x+2,y-1),1.5f,Colors.Black); }
                    break;
                case 5: // Collapsed burial room: scattered chips around a sunken mosaic.
                    DrawRect(new Rect2(358,134,308,308),dust,false,3);
                    for(int i=0;i<26;i++)
                    { var p=new Vector2(300+(i*97)%420,85+(i*67)%405); DrawColoredPolygon(new[]{p,p+new Vector2(7,-3),p+new Vector2(11,5),p+new Vector2(2,7)},new Color(.25f,.27f,.25f,.65f)); }
                    break;
            }
        }
        private void DrawCryptObstacle(Rect2 rect)
        {
            DrawRect(rect.Grow(-4), new Color(.21f,.22f,.24f));
            DrawRect(rect.Grow(-7), new Color(.40f,.39f,.34f), false, 2);
            var at=rect.GetCenter();
            if (Design == 2 || Design == 1)
            {
                DrawLine(at-new Vector2(0,12),at+new Vector2(0,14),new Color(.58f,.54f,.40f),3);
                DrawLine(at-new Vector2(9,4),at+new Vector2(9,-4),new Color(.58f,.54f,.40f),3);
            }
            else
            {
                DrawCircle(at,7,new Color(.55f,.51f,.40f));
                DrawCircle(at+new Vector2(-3,-1),2,new Color(.07f,.06f,.06f));
                DrawCircle(at+new Vector2(3,-1),2,new Color(.07f,.06f,.06f));
                DrawLine(at+new Vector2(-3,5),at+new Vector2(3,5),new Color(.07f,.06f,.06f),2);
            }
        }

        private void DrawCastleFloor()
        {
            // Marble and a woven cross runner distinguish the upper castle from the crypt.
            for (int x = 0; x < 10; x++)
            for (int y = 0; y < 10; y++)
            {
                var at = new Vector2(272 + x * 48, 48 + y * 48);
                Color tile = (x + y) % 2 == 0 ? new Color(0.29f, 0.28f, 0.25f) : new Color(0.22f, 0.23f, 0.24f);
                DrawRect(new Rect2(at + Vector2.One, new Vector2(46, 46)), tile);
                DrawLine(at + new Vector2(5, 35), at + new Vector2(32, 12), new Color(0.55f, 0.52f, 0.43f, 0.1f));
            }
            Color cloth = Design % 2 == 0 ? new Color(0.31f, 0.075f, 0.13f) : new Color(0.08f, 0.17f, 0.27f);
            Color trim = new Color(0.65f, 0.48f, 0.2f, 0.8f);
            if (Design == 1)
            {
                foreach (var at in new[] { new Vector2(416,192), new Vector2(608,192), new Vector2(416,384), new Vector2(608,384) })
                    DrawArc(at, 52, 0, Mathf.Tau, 32, trim, 3);
                DrawRect(new Rect2(484, 64, 56, 448), cloth);
                return;
            }
            if (Design == 2)
            {
                for (int y = 105; y < 500; y += 75)
                    DrawColoredPolygon(new[] { new Vector2(512,y-20), new Vector2(542,y), new Vector2(512,y+20), new Vector2(482,y) }, trim);
                return;
            }
            if (Design == 3)
            {
                DrawRect(new Rect2(315, 100, 150, 375), cloth);
                DrawRect(new Rect2(559, 100, 150, 375), cloth);
                DrawRect(new Rect2(321, 106, 138, 363), trim, false, 2);
                DrawRect(new Rect2(565, 106, 138, 363), trim, false, 2);
                return;
            }
            if (Design == 4)
            {
                for (int y = 120; y < 480; y += 120)
                {
                    DrawRect(new Rect2(305, y, 414, 46), cloth);
                    DrawLine(new Vector2(310,y+5), new Vector2(714,y+5), trim, 2);
                }
                return;
            }
            if (Design == 5)
            {
                DrawArc(new Vector2(512,288), 160, 0, Mathf.Tau, 64, trim, 4);
                DrawArc(new Vector2(512,288), 148, 0, Mathf.Tau, 64, cloth, 12);
                for (int i = 0; i < 8; i++)
                {
                    var at = new Vector2(512,288) + Vector2.Right.Rotated(i * Mathf.Tau / 8) * 115;
                    DrawRect(new Rect2(at - Vector2.One * 6, Vector2.One * 12), trim);
                }
                return;
            }
            DrawRect(new Rect2(482, 64, 60, 448), cloth);
            DrawRect(new Rect2(288, 258, 448, 60), cloth);
            DrawRect(new Rect2(486, 68, 52, 440), trim, false, 2);
            DrawRect(new Rect2(292, 262, 440, 52), trim, false, 2);
            DrawArc(new Vector2(512, 288), 20, 0, Mathf.Tau, 32, trim, 2);
        }

        private void DrawBanner(Vector2 at)
        {
            DrawRect(new Rect2(at - new Vector2(18, 3), new Vector2(36, 4)), new Color(0.75f, 0.57f, 0.24f));
            var cloth = new Vector2[] { at + new Vector2(-14, 2), at + new Vector2(14, 2),
                at + new Vector2(14, 40), at + new Vector2(0, 50), at + new Vector2(-14, 40) };
            DrawColoredPolygon(cloth, new Color(0.42f, 0.09f, 0.18f));
            Color gold = new Color(0.88f, 0.68f, 0.3f);
            DrawLine(at + new Vector2(-9, 16), at + new Vector2(-6, 29), gold, 3);
            DrawLine(at + new Vector2(9, 16), at + new Vector2(6, 29), gold, 3);
            DrawLine(at + new Vector2(0, 12), at + new Vector2(0, 29), gold, 3);
            DrawLine(at + new Vector2(-6, 29), at + new Vector2(6, 29), gold, 3);
        }

        private void DrawWindow(Vector2 at)
        {
            DrawRect(new Rect2(at - new Vector2(8, 24), new Vector2(16, 48)), new Color(0.04f, 0.07f, 0.12f));
            DrawRect(new Rect2(at - new Vector2(5, 21), new Vector2(10, 42)), new Color(0.29f, 0.58f, 0.72f, 0.8f));
            DrawLine(at + new Vector2(0, -22), at + new Vector2(0, 22), new Color(0.65f, 0.5f, 0.25f), 2);
            DrawLine(at + new Vector2(-7, 0), at + new Vector2(7, 0), new Color(0.65f, 0.5f, 0.25f), 2);
        }

        private void DrawTorch(Vector2 at, float phase)
        {
            float flicker = Mathf.Sin(_time * 15 + phase);
            DrawCircle(at, 18 + flicker * 2, new Color(1, 0.65f, 0.2f, Castle ? 0.16f : 0.08f));
            DrawRect(new Rect2(at + new Vector2(-3, 1), new Vector2(6, 12)), new Color(0.2f, 0.13f, 0.17f));
            DrawRect(new Rect2(at + new Vector2(-5, 1), new Vector2(10, 3)), new Color(0.6f, 0.45f, 0.3f));
            DrawRect(new Rect2(at + new Vector2(-4, -6 - flicker), new Vector2(8, 8 + flicker)), new Color(1, Castle ? 0.6f : 0.35f, 0.12f));
            DrawRect(new Rect2(at + new Vector2(-2, -8 + flicker), new Vector2(4, 8 - flicker)), new Color(1, 0.85f, 0.35f));
            float spark = (_time * 12 + phase * 10) % 14;
            DrawRect(new Rect2(at + new Vector2(Mathf.Sin(_time * 4 + phase) * 4, -9 - spark), Vector2.One * 2),
                new Color(1, 0.7f, 0.25f, 1 - spark / 14));
        }
    }
}
