extends SceneTree
# Exercises the actual boss scheduler and projectiles, without a player death
# interrupting the encounter. Run with --headless --fixed-fps 60.
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

func await_stage(boss, pattern: String, stage: String, max_frames := 3000) -> bool:
 for i in range(max_frames):
  if boss.GetAttackPattern() == pattern and boss.GetAttackStage() == stage:
   return true
  await process_frame
 verify(false, "Timed out waiting for " + pattern + " / " + stage)
 return false

func clear_shots() -> void:
 for projectile in get_nodes_in_group("projectiles"):
  projectile.queue_free()
 await frames(2)

func _initialize() -> void:
 call_deferred("run")

func run() -> void:
 var arena := Node2D.new()
 root.add_child(arena)
 current_scene = arena
 var target := Node2D.new()
 arena.add_child(target)
 target.add_to_group("player")
 target.position = Vector2(650, 390)
 var boss = load("res://Scenes/Enemies/CastleBoss.tscn").instantiate()
 boss.AttackSeed = 1042
 boss.position = Vector2(512, 288)
 arena.add_child(boss)
 await frames(3)
 verify(boss.GetMaxHealth() == 140.0, "Final boss has its own harder health budget")
 verify(boss.GetCombatPhase() == 1, "Final boss starts in phase one")
 verify(boss.is_in_group("final_boss"), "Final boss has distinct identity")
 verify(boss.get_node("Visual/Sprite").sprite_frames.has_animation("death"), "Final boss retains animated defeat")

 await await_stage(boss, "Fan", "Telegraph")
 await clear_shots()
 var before_fan: int = boss.GetTotalShots()
 verify(boss.GetAttackTimeRemaining() > 0, "Fan is telegraphed before its damage")
 var warning_time: float = boss.GetAttackTimeRemaining()
 verify(warning_time <= .401, "Fan has a shorter warning")
 var frozen_pos: Vector2 = boss.position
 paused = true
 await frames(30)
 verify(is_equal_approx(boss.GetAttackTimeRemaining(), warning_time), "Pause freezes the attack timer")
 verify(boss.position == frozen_pos and boss.GetTotalShots() == before_fan, "Pause prevents movement and shots")
 paused = false
 await await_stage(boss, "Fan", "Attacking")
 await frames(3)
 var directions: Array[float] = []
 for projectile in get_nodes_in_group("projectiles"):
  directions.append(projectile.rotation)
 verify(directions.size() == 5, "First-phase fan fires five projectiles")
 directions.sort()
 verify(directions.size() == 5 and directions[-1] - directions[0] > 0.9, "Fan covers distinct aimed directions")
 await await_stage(boss, "Fan", "Recovery")
 verify(boss.GetTotalShots() - before_fan == 10, "First-phase fan has two finite volleys")
 await clear_shots()

 await await_stage(boss, "Ring", "Telegraph")
 verify(boss.GetAttackTimeRemaining() <= .181, "Ring has only a brief cast animation")
 await clear_shots()
 var safe_direction: Vector2 = boss.GetLockedAim()
 var before_ring: int = boss.GetTotalShots()
 await await_stage(boss, "Ring", "Attacking")
 await frames(3)
 var ring_count := 0
 var safe_gap_clear := true
 var rear_shots := 0
 for projectile in get_nodes_in_group("projectiles"):
  var direction := Vector2.RIGHT.rotated(projectile.rotation)
  ring_count += 1
  safe_gap_clear = safe_gap_clear and absf(safe_direction.angle_to(direction)) >= deg_to_rad(36) - 0.001
  if safe_direction.dot(direction) < 0:
   rear_shots += 1
 verify(ring_count >= 13 and ring_count <= 16, "Ring fires in multiple directions around the boss")
 verify(safe_gap_clear and rear_shots >= 8, "Radial attack preserves its dodge opening")
 await await_stage(boss, "Ring", "Recovery")
 verify(boss.GetTotalShots() - before_ring == ring_count, "First-phase ring fires one finite wave")
 await clear_shots()

 await await_stage(boss, "Charge", "Telegraph")
 verify(boss.GetAttackTimeRemaining() <= .451, "Charge has a shorter warning")
 var charge_direction: Vector2 = boss.GetLockedAim()
 var origin: Vector2 = boss.position
 target.position = boss.position - charge_direction * 150
 await await_stage(boss, "Charge", "Attacking")
 await frames(12)
 verify(boss.get_node("CollisionShape2D").scale.x > 1.3, "Charge has a larger physical impact area")
 var moved: Vector2 = boss.position - origin
 verify(moved.length() > 40 and moved.normalized().dot(charge_direction) > 0.99, "Charge follows the locked warning direction after target dodges")
 await await_stage(boss, "Charge", "Recovery")
 verify(absf(boss.position.x - 512) <= 181 and absf(boss.position.y - 288) <= 181, "Longer charge ends inside rectangular arena bounds")
 var counts: Dictionary = boss.GetAttackCounts()
 verify(counts["Fan"] >= 1 and counts["Ring"] >= 1 and counts["Charge"] >= 1, "All three different attacks execute")

 boss.TakeDamage(71)
 await frames(2)
 verify(boss.GetCombatPhase() == 2 and boss.GetCurrentHealth() == 69, "Half-health threshold activates second phase")
 await clear_shots()
 await await_stage(boss, "Fan", "Telegraph")
 var phase_two_start: int = boss.GetTotalShots()
 await await_stage(boss, "Fan", "Attacking")
 await await_stage(boss, "Fan", "Recovery")
 verify(boss.GetTotalShots() - phase_two_start == 21, "Second-phase fan adds a third seven-shot volley")
 await clear_shots()
 await await_stage(boss, "Ring", "Telegraph")
 var phase_two_ring: int = boss.GetTotalShots()
 await await_stage(boss, "Ring", "Attacking")
 await await_stage(boss, "Ring", "Recovery")
 verify(boss.GetTotalShots() - phase_two_ring >= 36, "Second-phase ring adds a second wave")

 var shots = get_nodes_in_group("projectiles")
 verify(not shots.is_empty(), "Live boss shots exist before defeat")
 if not shots.is_empty():
  var shot = shots[0]
  var shot_pos: Vector2 = shot.position
  paused = true
  await frames(20)
  verify(is_instance_valid(shot) and shot.position == shot_pos, "Pause freezes active projectiles")
  paused = false
 var shots_before_death: int = boss.GetTotalShots()
 boss.TakeDamage(1000)
 verify(boss.GetAttackStage() == "Defeated" and not boss.is_physics_processing(), "Death immediately stops the attack scheduler")
 verify(boss.collision_layer == 0 and not boss.is_in_group("enemies"), "Dead boss cannot deal contact damage")
 await frames(4)
 verify(get_nodes_in_group("projectiles").is_empty(), "Final defeat clears the boss's remaining shots")
 verify(boss.GetTotalShots() == shots_before_death, "No post-death volley is spawned")
 await frames(40)
 verify(not is_instance_valid(boss), "Defeat cleans up the final boss")

 # A real player body must interrupt a charge immediately. The enemy's shared
 # recoil/recovery must not resume the interrupted charge half a second later.
 target.queue_free()
 await frames(2)
 var player = load("res://Scenes/Player/Player.tscn").instantiate()
 player.position = Vector2(650, 288)
 player.HurtInvincibilitySeconds = 10000.0
 arena.add_child(player)
 player.TakeDamage(1)
 boss = load("res://Scenes/Enemies/CastleBoss.tscn").instantiate()
 boss.AttackSeed = 1042
 boss.position = Vector2(512, 288)
 arena.add_child(boss)
 await await_stage(boss, "Charge", "Telegraph")
 player.position = boss.position + boss.GetLockedAim() * 138
 await clear_shots()
 await await_stage(boss, "Charge", "Attacking")
 var charge_start: Vector2 = boss.position
 await await_stage(boss, "Charge", "Recovery")
 verify(boss.position.distance_to(charge_start) > 70 and boss.position.distance_to(player.position) > 35, "Real player collision ends charge before its full travel")
 var contact_gap: float = player.position.distance_to(boss.position)
 await frames(16)
 verify(player.position.distance_to(boss.position) > contact_gap + 25, "Final boss contact opens space through shared recoil")
 await frames(20)
 verify(boss.GetAttackStage() == "Recovery" and boss.GetAttackCounts()["Charge"] == 1, "Interrupted charge stays finished after contact recovery")
 verify(player.GetCurrentHealthHalfHearts() == 7, "Invulnerable player contact cannot bypass immunity")
 boss.TakeDamage(1000)
 await frames(40)
 print("FINAL BOSS CHECKS: ", passed, " passed; ", failed, " failed")
 await preload("res://Tests/AudioTestCleanup.gd").drain(self)
 quit(1 if failed else 0)
