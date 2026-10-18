extends "res://Tests/RoguelikeSmoke.gd"

func run() -> void:
 manager = root.get_node("DungeonManager")
 var arena := Node2D.new()
 root.add_child(arena)
 current_scene = arena
 var target := Node2D.new()
 target.position = Vector2(900, 288)
 target.add_to_group("player")
 arena.add_child(target)
 for kind in ["EnemyRanged", "EnemyFourWay", "BossRanged", "BossFourWay", "Boss", "CastleBoss"]:
  var enemy = load("res://Scenes/Enemies/" + kind + ".tscn").instantiate()
  enemy.position = Vector2(512,288)
  enemy.HeartDropChance = 0
  arena.add_child(enemy)
  await frames(40)
  verify(get_nodes_in_group("projectiles").is_empty(), kind + " never fires immediately on entering")
  var grace: float = enemy.GetEntryGraceRemaining()
  paused = true
  await frames(30)
  verify(is_equal_approx(enemy.GetEntryGraceRemaining(), grace), "Pausing cannot consume the entry protection")
  paused = false
  await frames(46)
  verify(get_nodes_in_group("projectiles").is_empty(), kind + " waits at least 1.5 seconds before firing")
  var fired := false
  for i in range(200):
   await frames(1)
   if not get_nodes_in_group("projectiles").is_empty():
    fired = true
    break
  verify(fired, kind + " still attacks after the grace interval")
  for shot in get_nodes_in_group("projectiles"): shot.queue_free()
  enemy.ResetEntryGrace()
  await frames(85)
  verify(get_nodes_in_group("projectiles").is_empty(), kind + " gets a fresh grace interval on reactivation")
  enemy.queue_free()
  for shot in get_nodes_in_group("projectiles"): shot.queue_free()
  await frames(3)
 var previous_speed := 0.0
 var previous_interval := 100.0
 var previous_bullet := 0.0
 for floor_number in range(1,7):
  var enemy = load("res://Scenes/Enemies/EnemyRanged.tscn").instantiate()
  enemy.ConfigureForFloor(floor_number, false)
  var interval: float = enemy.FireCooldownSeconds * enemy.ShotIntervalMultiplier
  var speed: float = enemy.ProjectileSpeed * enemy.ProjectileSpeedMultiplier
  verify(enemy.MoveSpeed > previous_speed and enemy.MoveSpeed < 150, "Movement/evasion increases each floor while remaining escapable")
  verify(interval < previous_interval and interval >= 1.0, "Shots become progressively more frequent")
  verify(speed > previous_bullet, "Projectile speed increases each floor")
  if floor_number == 1:
   verify(interval > 1.4 and speed < 220 and enemy.MoveSpeed < 85, "First floor is gentler than original baseline")
  previous_speed = enemy.MoveSpeed
  previous_interval = interval
  previous_bullet = speed
  enemy.free()
 target.queue_free()
 await frames(2)
 manager.SeedOverride = 2026
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(4)
 protect_player()
 graph = index_graph(manager.GetFloorSnapshot())
 var layouts := {}
 for entry in graph.values():
  if entry.kind == "Normal": layouts[entry.layout] = entry.id
 var starts := [Vector2(352,288), Vector2(352,192), Vector2(344,288), Vector2(352,240), Vector2(324,232), Vector2(352,288)]
 var goals := [Vector2(560,288), Vector2(480,192), Vector2(488,288), Vector2(480,240), Vector2(448,232), Vector2(464,288)]
 for layout in range(6):
  await navigate(layouts[layout])
  await clear_room()
  player.position = goals[layout]
  var enemy = load("res://Scenes/Enemies/EnemyMelee.tscn").instantiate()
  enemy.position = starts[layout]
  enemy.HeartDropChance = 0
  manager.GetCurrentRoom().get_node("Enemies").add_child(enemy)
  var closest := 1000.0
  var detour := 0.0
  for i in range(480):
   await frames(1)
   closest = minf(closest, enemy.position.distance_to(player.position))
   detour = maxf(detour, abs(enemy.position.y - starts[layout].y))
   if closest < 38: break
  verify(closest < 38, "Melee reaches player around layout " + str(layout))
  if layout != 0: verify(detour > 18, "Melee takes a real alternate route around solid structure " + str(layout))
  enemy.queue_free()
  await frames(3)
 # Reproduce an enemy already pressed flat against the side of a pillar.
 await navigate(layouts[1])
 player.position = Vector2(480,192)
 var stuck = load("res://Scenes/Enemies/EnemyMelee.tscn").instantiate()
 stuck.position = Vector2(378,192)
 stuck.HeartDropChance = 0
 manager.GetCurrentRoom().get_node("Enemies").add_child(stuck)
 var escaped := false
 for i in range(480):
  await frames(1)
  if stuck.position.distance_to(player.position) < 38:
   escaped = true
   break
 verify(escaped, "Already-stuck melee escapes pillar contact instead of freezing")
 player.position = Vector2(352,192)
 var followed := false
 for i in range(480):
  await frames(1)
  if stuck.position.distance_to(player.position) < 38:
   followed = true
   break
 verify(followed, "Melee recalculates its route when the player changes sides")
 stuck.queue_free()
 print("COMBAT SAFETY CHECKS: ", passed, " passed; ", failed, " failed")
 await preload("res://Tests/AudioTestCleanup.gd").drain(self)
 quit(1 if failed else 0)
