extends SceneTree
# Real CharacterBody2D collisions, run with --headless --fixed-fps 60.
var passed := 0
var failed := 0
var arena: Node2D

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

func create_arena() -> void:
 arena = Node2D.new()
 root.add_child(arena)
 current_scene = arena

func player_at(at: Vector2):
 var player = load("res://Scenes/Player/Player.tscn").instantiate()
 player.position = at
 arena.add_child(player)
 return player

func enemy_at(scene_name: String, at: Vector2):
 var enemy = load("res://Scenes/Enemies/" + scene_name + ".tscn").instantiate()
 enemy.position = at
 enemy.HeartDropChance = 0.0
 arena.add_child(enemy)
 return enemy

func wall_at(at: Vector2, size: Vector2) -> void:
 var wall := StaticBody2D.new()
 wall.position = at
 wall.collision_layer = 1
 wall.collision_mask = 0
 var shape := CollisionShape2D.new()
 var rectangle := RectangleShape2D.new()
 rectangle.size = size
 shape.shape = rectangle
 wall.add_child(shape)
 arena.add_child(wall)

func wait_for_hit(player, max_frames: int = 120) -> bool:
 var health: int = player.GetCurrentHealthHalfHearts()
 for i in range(max_frames):
  await frames(1)
  if player.GetCurrentHealthHalfHearts() < health:
   return true
 return false

func clear_arena() -> void:
 for action in ["move_up", "move_down", "move_left", "move_right", "dash"]:
  Input.action_release(action)
 arena.queue_free()
 await frames(3)

func run() -> void:
 for scene_name in ["EnemyMelee", "BossMelee"]:
  create_arena()
  var player = player_at(Vector2(400, 300))
  var enemy = enemy_at(scene_name, Vector2(280, 300))
  verify(await wait_for_hit(player), scene_name + " makes real contact")
  var hit_health: int = player.GetCurrentHealthHalfHearts()
  verify(hit_health == 8 - enemy.ContactDamageHalfHearts, scene_name + " deals exactly one contact hit")
  verify(enemy.get_node("Visual/Sprite").animation == &"attack", scene_name + " contact plays attack")
  var enemy_at_hit: Vector2 = enemy.position
  var gap_at_hit: float = player.position.distance_to(enemy.position)
  await frames(10)
  verify(enemy.position.x < enemy_at_hit.x - 18, scene_name + " physically recoils away")
  verify(player.position.distance_to(enemy.position) > gap_at_hit + 45, scene_name + " opens an escape gap")
  verify(player.GetCurrentHealthHalfHearts() == hit_health, scene_name + " does not repeat damage during recoil")
  await frames(7)
  var recovery_position: Vector2 = enemy.position
  await frames(8)
  verify(enemy.position.distance_to(recovery_position) < 0.1, scene_name + " rests briefly after recoil")
  await frames(15)
  verify(enemy.position.x > recovery_position.x + 5, scene_name + " resumes pursuit after recovery")
  var escape_gap: float = player.position.distance_to(enemy.position)
  Input.action_press("move_right")
  await frames(45)
  Input.action_release("move_right")
  verify(player.position.distance_to(enemy.position) > escape_gap + 30, scene_name + " lets normal movement escape")
  verify(player.GetCurrentHealthHalfHearts() == hit_health, scene_name + " does not damage an escaping player again")
  enemy.TakeDamage(10000)
  await frames(1)
  verify(not enemy.is_physics_processing() and enemy.collision_layer == 0, scene_name + " death still disables contact immediately")
  await clear_arena()

 # A player pinned against a wall still gets space because the enemy recoils.
 create_arena()
 wall_at(Vector2(460, 300), Vector2(20, 300))
 var player = player_at(Vector2(438, 300))
 var enemy = enemy_at("EnemyMelee", Vector2(370, 300))
 verify(await wait_for_hit(player), "Wall case makes real contact")
 var enemy_at_hit: Vector2 = enemy.position
 var wall_health: int = player.GetCurrentHealthHalfHearts()
 await frames(17)
 verify(player.position.x <= 438.2, "Player knockback respects the wall")
 verify(enemy.position.x < enemy_at_hit.x - 25, "Enemy retreats even when player knockback is blocked")
 Input.action_press("move_up")
 await frames(20)
 Input.action_release("move_up")
 verify(player.position.y < 265, "Player can escape along the wall after the hit")
 verify(player.GetCurrentHealthHalfHearts() == wall_health, "Wall escape avoids repeated damage")
 await clear_arena()

 # A wall behind the enemy redirects its retreat without teleporting through it.
 create_arena()
 wall_at(Vector2(352, 300), Vector2(16, 300))
 player = player_at(Vector2(410, 300))
 enemy = enemy_at("EnemyMelee", Vector2(374.1, 300))
 verify(await wait_for_hit(player), "Enemy-wall case makes real contact")
 await frames(17)
 verify(enemy.position.x >= 373.9, "Enemy recoil cannot cross a solid wall")
 verify(absf(enemy.position.y - 300) > 20, "Enemy chooses a clear sidestep when retreat is blocked")
 await clear_arena()

 # Contact during hurt immunity must not restart the player's forced movement.
 create_arena()
 player = player_at(Vector2(400, 300))
 player.HurtInvincibilitySeconds = 2.0
 player.TakeDamage(1)
 enemy = enemy_at("EnemyMelee", Vector2(366, 300))
 await frames(18)
 verify(enemy.position.x < 350, "Immune contact still makes the enemy retreat")
 verify(player.position.distance_to(Vector2(400, 300)) < 0.1, "Immune contact does not knock the player back")
 await frames(75)
 verify(player.GetCurrentHealthHalfHearts() == 7, "Repeated contact cannot bypass hurt immunity")
 # Floating bodies may separate by two 0.08px safe margins; this is not forced knockback.
 verify(player.position.distance_to(Vector2(400, 300)) < 0.25, "Repeated immune contacts cannot lock the player in knockback")
 Input.action_press("move_down")
 await frames(12)
 Input.action_release("move_down")
 verify(player.position.y > 325, "Input remains responsive during hurt immunity")
 await clear_arena()

 # Dashing into a body can collide, but an immune contact cannot reverse the dash.
 create_arena()
 player = player_at(Vector2(400, 300))
 enemy = enemy_at("EnemyMelee", Vector2(450, 300))
 Input.action_press("move_right")
 Input.action_press("dash")
 await frames(12)
 Input.action_release("dash")
 Input.action_release("move_right")
 verify(player.GetCurrentHealthHalfHearts() == 8, "Dash immunity prevents melee damage")
 verify(player.position.x > 415, "Contact does not push an invulnerable dash backward")
 verify(enemy.position.x > 450, "Dashing contact separates the enemy")
 await clear_arena()

 print("MELEE CONTACT CHECKS: ", passed, " passed; ", failed, " failed")
 await preload("res://Tests/AudioTestCleanup.gd").drain(self)
 quit(0 if failed == 0 else 1)
