extends SceneTree
# Run with Godot 4.3 .NET: --headless --fixed-fps 60 --path . --script res://Tests/AnimationSmoke.gd
var passed := 0
var failed := 0

func verify(condition: bool, description: String) -> void:
 if condition:
  passed += 1
 else:
  failed += 1
  push_error(description)

func frames(count: int) -> void:
 for i in range(count):
  await process_frame

func _initialize() -> void:
 call_deferred("run")

func run() -> void:
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(3)
 var game := current_scene
 var player = get_first_node_in_group("player")
 if game == null or player == null:
  push_error("Main scene or C# player did not initialize")
  quit(1)
  return
 var visual = player.get_node("Visual")
 var sprite: AnimatedSprite2D = visual.get_node("Sprite")
 verify(visual.GetAnimationState() == "idle", "Player starts idle")
 var first_texture = sprite.sprite_frames.get_frame_texture("idle", 0)
 verify(first_texture != sprite.sprite_frames.get_frame_texture("idle", 1), "Idle has distinct poses")
 var old_pos: Vector2 = player.position
 Input.action_press("move_left")
 await frames(6)
 verify(player.position.x < old_pos.x, "Movement still moves the player")
 verify(visual.GetAnimationState() == "walk", "Movement triggers walk")
 verify(sprite.animation == &"walk_left" and not sprite.flip_h, "Walking left uses its own staff poses")
 Input.action_release("move_left")
 await frames(3)
 verify(visual.GetAnimationState() == "idle", "Releasing movement returns to idle")
 Input.action_press("aim_right")
 await frames(3)
 verify(visual.GetAnimationState() == "attack", "Firing triggers attack")
 verify(sprite.animation == &"attack_right" and not sprite.flip_h, "Aiming right selects right-facing staff casting")
 Input.action_release("aim_right")
 await frames(20)
 verify(visual.GetAnimationState() == "idle", "Attack returns to idle")
 Input.action_press("dash")
 await frames(3)
 Input.action_release("dash")
 verify(visual.GetAnimationState() == "dash", "Dash triggers dash animation")
 await frames(25)
 verify(visual.GetAnimationState() == "idle", "Dash returns to idle")
 player.TakeDamage(1)
 await frames(2)
 verify(visual.GetAnimationState() == "hurt", "Damage triggers hurt")
 verify(player.GetCurrentHealthHalfHearts() == 7, "Damage applies once")
 await frames(20)
 verify(visual.GetAnimationState() == "idle", "Hurt returns to idle")
 var paused_frame: int = sprite.frame
 var paused_progress: float = sprite.frame_progress
 paused = true
 await frames(20)
 verify(sprite.frame == paused_frame and is_equal_approx(sprite.frame_progress, paused_progress), "Living actors freeze their animation during pause")
 paused = false

 for name in ["EnemyMelee", "EnemyRanged", "EnemyFourWay", "Boss", "BossMelee", "BossRanged", "BossFourWay"]:
  var enemy = load("res://Scenes/Enemies/" + name + ".tscn").instantiate()
  enemy.HeartDropChance = 0.0
  game.add_child(enemy)
  enemy.set_physics_process(false)
  enemy.collision_layer = 0
  enemy.collision_mask = 0
  enemy.position = Vector2(550, 250)
  var art = enemy.get_node("Visual")
  var animated: AnimatedSprite2D = art.get_node("Sprite")
  for state in ["idle", "walk", "attack", "hurt", "death", "dash"]:
   verify(animated.sprite_frames.has_animation(state), name + " has " + state)
  art.SetMotion(Vector2(50, 0), Vector2.RIGHT)
  await frames(2)
  verify(art.GetAnimationState() == "walk", name + " walk transition")
  var initial_frame: int = animated.frame
  await frames(8)
  verify(animated.frame != initial_frame, name + " advances animation frames")
  art.PlayAttack(Vector2.RIGHT)
  art.SetMotion(Vector2.LEFT * 50, Vector2.LEFT)
  await frames(2)
  verify(art.GetAnimationState() == "attack", name + " attack transition")
  verify(not animated.flip_h, name + " faces its attack while moving away")
  art.PlayAttack(Vector2.RIGHT)
  verify(animated.frame == 0, name + " repeated attack restarts from windup")
  enemy.TakeDamage(1)
  await frames(2)
  if name.begins_with("Boss"):
   verify(art.GetAnimationState() != "hurt" and animated.self_modulate.r > animated.self_modulate.g * 2, name + " flashes red without hurt pose")
  else:
   verify(art.GetAnimationState() == "hurt", name + " hurt has priority over attack")
  enemy.TakeDamage(10000)
  await frames(2)
  verify(art.GetAnimationState() == "death", name + " death transition")
  verify(not enemy.is_physics_processing() and enemy.collision_layer == 0, name + " death disables combat immediately")
  verify(not enemy.is_in_group("enemies"), name + " dead actor leaves enemies group")
  await frames(19)
  verify(animated.frame == 1, name + " reaches defeated pose")
  await frames(20)
  verify(not is_instance_valid(enemy), name + " death cleans up node")

 # Real firing behaviors, after the room-entry grace interval.
 var player_layer: int = player.collision_layer
 player.collision_layer = 0
 for name in ["EnemyRanged", "EnemyFourWay", "Boss", "BossRanged", "BossFourWay"]:
  var enemy = load("res://Scenes/Enemies/" + name + ".tscn").instantiate()
  if name == "Boss": enemy.ChasePhaseSeconds = 0.01
  game.add_child(enemy)
  enemy.position = Vector2(650, 150)
  enemy.collision_layer = 0
  enemy.collision_mask = 0
  for frame in range(180):
   await frames(1)
   if enemy.get_node("Visual").GetAnimationState() == "attack": break
  verify(enemy.get_node("Visual").GetAnimationState() == "attack", name + " actual shot triggers attack after entry delay")
  enemy.queue_free()
  await frames(2)
 player.collision_layer = player_layer
 # Remove projectiles from the firing checks before collection checks.
 for node in game.get_children():
  if node is Area2D:
   node.queue_free()
 await frames(2)

 for name in ["Item01_ExtraHeart", "Item02_MoveSpeed", "Item03_FireRate", "Item04_Damage", "Item05_Piercing", "Item06_ArcaneLens"]:
  var data = load("res://Resources/Items/" + name + ".tres")
  var item = load("res://Scenes/Items/ItemPickup.tscn").instantiate()
  item.Data = data
  game.add_child(item)
  item.position = Vector2(600, 400)
  var item_visual = item.get_node("Visual")
  var at: Vector2 = item_visual.position
  await frames(10)
  verify(item_visual.position != at, name + " floats")
  verify(item.get_node("Visual/Sprite").texture == data.Icon, name + " retains its own icon")
  var speed_before: float = player.MoveSpeed
  item.emit_signal("body_entered", player)
  item.emit_signal("body_entered", player)
  if name == "Item02_MoveSpeed":
   verify(is_equal_approx(player.MoveSpeed, speed_before * (1 + data.EffectValue / 100)), "Collection effect applies once during animation")
  await frames(6)
  verify(is_instance_valid(item) and item_visual.modulate.a < 1, name + " plays collection before removal")
  await frames(20)
  verify(not is_instance_valid(item), name + " collection cleans up")

 player.TakeDamage(3)
 await frames(20)
 var health_before: int = player.GetCurrentHealthHalfHearts()
 var heart = load("res://Scenes/Items/HeartPickup.tscn").instantiate()
 game.add_child(heart)
 heart.position = Vector2(600, 400)
 heart.emit_signal("body_entered", player)
 heart.emit_signal("body_entered", player)
 verify(player.GetCurrentHealthHalfHearts() == mini(health_before + 2, player.GetMaxHealthHalfHearts()), "Heart heals once during animation")
 await frames(25)
 verify(not is_instance_valid(heart), "Heart collection cleans up")

 for name in ["PlayerProjectile", "EnemyProjectile"]:
  var projectile = load("res://Scenes/Projectiles/" + name + ".tscn").instantiate()
  projectile.LifetimeSeconds = 0.2
  game.add_child(projectile)
  projectile.position = Vector2(500, 380)
  projectile.Launch(Vector2.RIGHT, 100, 1)
  var shot_sprite = projectile.get_node("Sprite")
  var base_scale: Vector2 = shot_sprite.scale
  await frames(5)
  verify(shot_sprite.scale != base_scale, name + " pulses in flight")
  verify(projectile.position.x > 500, name + " keeps moving")
  await frames(12)
  verify(not is_instance_valid(projectile), name + " expires and cleans up")

 var room = root.get_node("DungeonManager").GetCurrentRoom()
 var door = room.get_node("Doors").get_child(0)
 door.SetClosed()
 await frames(22)
 var material: ShaderMaterial = door.get_node("DoorSprite").material
 verify(is_zero_approx(material.get_shader_parameter("openness")), "Closed door has closed shader state")
 door.SetOpen()
 await frames(4)
 var middle: float = material.get_shader_parameter("openness")
 verify(middle > 0 and middle < 1, "Door animates through intermediate states")
 verify(door.GetIsOpen(), "Door becomes traversable independently of its visual")
 await frames(20)
 verify(is_equal_approx(material.get_shader_parameter("openness"), 1), "Opening completes")
 door.SetClosed()
 await frames(3)
 door.SetOpen()
 await frames(22)
 verify(is_equal_approx(material.get_shader_parameter("openness"), 1), "Door reverses without a stale tween")
 await frames(3)

 var melee = load("res://Scenes/Enemies/EnemyMelee.tscn").instantiate()
 melee.HeartDropChance = 0
 game.add_child(melee)
 melee.position = player.position + Vector2(50, 0)
 await frames(22)
 verify(melee.get_node("Visual").GetAnimationState() == "attack", "Real melee contact triggers attack animation")
 melee.queue_free()
 await frames(30)

 player.TakeDamage(100)
 await frames(3)
 verify(paused, "Player death still pauses gameplay")
 verify(visual.GetAnimationState() == "death", "Player death animation runs while paused")
 await frames(22)
 verify(sprite.frame == 1, "Player reaches defeated pose while game is paused")
 paused = false
 for node in root.get_children():
  if node is CanvasLayer:
   node.queue_free()
 change_scene_to_file("res://Scenes/UI/MainMenu.tscn")
 await frames(4)
 var hero = current_scene.get_node("CenterContainer/VBoxContainer/HeroArt")
 var menu_texture = hero.texture
 await frames(22)
 verify(hero.texture != menu_texture, "Menu hero advances idle poses")
 var button = current_scene.get_node("CenterContainer/VBoxContainer/PlayButton")
 button.grab_focus()
 await frames(10)
 verify(button.scale.x > 1, "Menu button animates keyboard focus")
 button.release_focus()
 await frames(10)
 verify(is_equal_approx(button.scale.x, 1), "Menu button returns to normal scale")
 print("ANIMATION CHECKS: ", passed, " passed; ", failed, " failed")
 await preload("res://Tests/AudioTestCleanup.gd").drain(self)
 quit(1 if failed > 0 else 0)
