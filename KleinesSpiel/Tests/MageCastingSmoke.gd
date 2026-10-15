extends SceneTree
# Run with Godot .NET: --headless --fixed-fps 60 --path . --script res://Tests/MageCastingSmoke.gd
var passed := 0
var failed := 0
func verify(ok: bool, description: String) -> void:
 if ok: passed += 1
 else:
  failed += 1
  push_error(description)
func frames(count: int) -> void:
 for i in range(count): await process_frame
func _initialize() -> void:
 call_deferred("run")
func run() -> void:
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(3)
 var player = get_first_node_in_group("player")
 var visual = player.get_node("Visual")
 var sprite: AnimatedSprite2D = visual.get_node("Sprite")
 var directions = {"down": Vector2.DOWN, "up": Vector2.UP, "left": Vector2.LEFT, "right": Vector2.RIGHT}
 var opposite_motion = {"down": "move_up", "up": "move_down", "left": "move_right", "right": "move_left"}
 var attack_resources := {}
 for name in directions:
  var direction: Vector2 = directions[name]
  for state in ["idle", "walk", "attack", "hurt", "death", "dash"]:
   verify(sprite.sprite_frames.has_animation(state + "_" + name), name + " has directional " + state)
  var release = sprite.sprite_frames.get_frame_texture("attack_" + name, 1)
  verify(release.atlas.resource_path == "res://Assets/Sprites/hero_mage_directional.png", name + " uses the new staff art")
  attack_resources[release.region] = true
  for node in current_scene.get_children():
   if node is Area2D: node.queue_free()
  await frames(30)
  player.position = Vector2(512, 288)
  Input.action_press(opposite_motion[name])
  Input.action_press("aim_" + name)
  await frames(3)
  Input.action_release("aim_" + name)
  Input.action_release(opposite_motion[name])
  verify(sprite.animation == StringName("attack_" + name), name + " actual shooting selects its casting animation")
  verify(not sprite.flip_h, name + " uses drawn poses without stale mirroring")
  verify(player.velocity.dot(direction) < 0, name + " casting stays oriented while moving backward")
  var shots: Array[Area2D] = []
  for node in current_scene.get_children():
   if node is Area2D and node.collision_layer == 8: shots.append(node)
  verify(shots.size() == 1, name + " produces one projectile")
  if shots.size() == 1:
   var shot = shots[0]
   verify(abs(wrapf(shot.rotation - direction.angle(), -PI, PI)) < 0.001, name + " projectile follows casting direction")
   var from_tip: Vector2 = shot.global_position - player.GetCastOrigin()
   verify(abs(from_tip.cross(direction)) < 0.5, name + " projectile follows the staff-tip line")
   verify(from_tip.dot(direction) > 0, name + " projectile travels outward from staff")
  var origin_offset: Vector2 = player.GetCastOrigin() - player.global_position
  verify(origin_offset.dot(direction) > 10, name + " muzzle is ahead of the character")
  await frames(20)
  verify(sprite.animation == StringName("idle_" + name), name + " keeps facing after firing stops")
 verify(attack_resources.size() == 4, "Four casting directions use different regions")
 # Switch direction while attack state is still active; the old implementation
 # only switched animation when the base state changed.
 visual.PlayAttack(Vector2.LEFT)
 await frames(1)
 verify(sprite.animation == &"attack_left", "Left cast can begin during another attack")
 visual.PlayAttack(Vector2.UP)
 await frames(1)
 verify(sprite.animation == &"attack_up", "Vertical cast replaces horizontal cast without idle transition")
 visual.PlayAttack(Vector2.RIGHT)
 await frames(1)
 verify(sprite.animation == &"attack_right" and sprite.frame == 0, "Rapid right cast restarts its own windup")
 # Facing also updates while the base locomotion state remains walk.
 await frames(20)
 for name in directions:
  Input.action_press("move_" + name)
  await frames(3)
  verify(sprite.animation == StringName("walk_" + name), name + " locomotion selects directional art")
  Input.action_release("move_" + name)
  await frames(2)
 print("MAGE CASTING CHECKS: ", passed, " passed; ", failed, " failed")
 quit(1 if failed > 0 else 0)
