using Godot;

namespace KleinesSpiel.Scripts.Rooms
{
	// Eine Tuer hat zwei Teile:
	// - "Blocker": eine feste Wand, die den Weg versperrt, solange die Tuer zu ist.
	// - "TriggerArea": eine Zone, die den Spieler in den naechsten Raum schickt,
	//   aber nur wenn die Tuer offen ist.
	public partial class Door : Node2D
	{
		// Welche Himmelsrichtung diese Tuer im Raum darstellt. Wird vom
		// DungeonManager benutzt, um die Gitterposition jeder Sala fuer die
		// Minimap zu berechnen (die Sala hinter einer "Up"-Tuer liegt direkt
		// ueber der aktuellen, usw.) - unabhaengig vom Kartenlayout selbst,
		// das weiterhin komplett ueber TargetRoomScenePath definiert wird.
		public enum Direction { Up, Down, Left, Right }

		[Export] public string TargetRoomScenePath = "";
		[Export] public string SpawnPointName = "";
		[Export] public Direction DoorDirection = Direction.Right;

		private bool _isOpen;
		private StaticBody2D _blocker;
		private CollisionShape2D _blockerShape;
		private Area2D _trigger;
		private ColorRect _visual;
		private Sprite2D _doorSprite;
		private Texture2D _openTexture;
		private Texture2D _closedTexture;
		private ShaderMaterial _doorMaterial;
		private Tween _doorTween;
		private bool _artInitialized;
		public bool GetIsOpen() => _isOpen;

		public override void _Ready()
		{
			_blocker = GetNodeOrNull<StaticBody2D>("Blocker");
			_blockerShape = _blocker?.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			_trigger = GetNodeOrNull<Area2D>("TriggerArea");
			_visual = GetNodeOrNull<ColorRect>("Visual");
			_doorSprite = GetNodeOrNull<Sprite2D>("DoorSprite");
			_openTexture = GD.Load<Texture2D>("res://Assets/Sprites/Tiles/door_open.svg");
			_closedTexture = GD.Load<Texture2D>("res://Assets/Sprites/Tiles/door_closed.svg");
			if (_doorSprite != null)
			{
				_doorMaterial = new ShaderMaterial
				{
					Shader = GD.Load<Shader>("res://Assets/Sprites/Tiles/door_transition.gdshader")
				};
				_doorMaterial.SetShaderParameter("open_texture", _openTexture);
				_doorSprite.Material = _doorMaterial;
				_doorSprite.Texture = _closedTexture;
			}

			if (_trigger != null)
			{
				_trigger.BodyEntered += OnBodyEntered;
			}

			CreateProjectileBlocker();

			SetClosed();
		}

		// Eine zusaetzliche, IMMER aktive Kollision (eigener Layer 64), die nur
		// von Projektilen beachtet wird (siehe PlayerProjectile/EnemyProjectile
		// collision_mask). Der Spieler benutzt diesen Layer nicht und laeuft
		// weiterhin ungehindert durch eine offene Tuer - aber Kugeln bleiben an
		// der Tueroeffnung haengen, statt geradeaus ins Leere weiterzufliegen.
		private void CreateProjectileBlocker()
		{
			if (_blockerShape == null || _blockerShape.Shape == null)
				return;

			var projectileBlocker = new StaticBody2D
			{
				Name = "ProjectileBlocker",
				CollisionLayer = 64,
				CollisionMask = 0,
			};
			AddChild(projectileBlocker);

			var shape = new CollisionShape2D
			{
				Shape = _blockerShape.Shape,
			};
			projectileBlocker.AddChild(shape);
		}

		public void SetOpen()
		{
			AnimateDoor(true);
			_isOpen = true;
			_blockerShape?.SetDeferred("disabled", true);
			if (_visual != null)
				_visual.Color = new Color(0.2f, 0.8f, 0.3f, 1f); // gruen = offen
		}

		public void SetClosed()
		{
			AnimateDoor(false);
			_isOpen = false;
			_blockerShape?.SetDeferred("disabled", false);
			if (_visual != null)
				_visual.Color = new Color(0.5f, 0.3f, 0.1f, 1f); // braun = zu
		}

		private void AnimateDoor(bool open)
		{
			if (_doorMaterial == null) return;
			if (!_artInitialized)
			{
				_artInitialized = true;
				_doorMaterial.SetShaderParameter("openness", open ? 1f : 0f);
				return;
			}
			if (_isOpen == open) return;
			_doorTween?.Kill();
			_doorTween = CreateTween();
			_doorTween.TweenProperty(_doorMaterial, "shader_parameter/openness", open ? 1f : 0f, 0.28f)
				.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			Effects.BurstEffect.Spawn(this, GlobalPosition,
				open ? new Color(0.4f, 1, 0.8f) : new Color(1, 0.7f, 0.3f), 18, 0.3f);
		}

		private void OnBodyEntered(Node2D body)
		{
			if (!_isOpen)
				return;

			if (body is Player.Player && !string.IsNullOrEmpty(TargetRoomScenePath))
			{
				DungeonManager.Instance?.ChangeRoom(TargetRoomScenePath, SpawnPointName, DoorDirection);
			}
		}
	}
}
