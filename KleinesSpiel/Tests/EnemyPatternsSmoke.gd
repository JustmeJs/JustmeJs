extends SceneTree
var passed := 0
var failed := 0
func verify(ok: bool, message: String):
 if ok: passed += 1
 else:
  failed += 1
  push_error(message)
func frames(n: int):
 for i in range(n): await process_frame
func _initialize(): call_deferred("run")
func run():
 var arena := Node2D.new()
 root.add_child(arena)
 current_scene = arena
 var target := Node2D.new()
 target.add_to_group("player")
 target.position = Vector2(650,288)
 arena.add_child(target)
 var archer = load("res://Scenes/Enemies/EnemyRanged.tscn").instantiate()
 archer.position = Vector2(400,288)
 arena.add_child(archer)
 await frames(95)
 var shots = get_nodes_in_group("projectiles")
 verify(shots.size() >= 1, "Archer actually fires")
 for shot in shots:
  verify(shot.IsArrow and not shot.get_node("Sprite").visible, "Archer uses directional arrow instead of red orb")
  verify(absf(shot.rotation) < .05, "Arrow points toward target")
  shot.queue_free()
 archer.queue_free()
 await frames(3)
 var boss = load("res://Scenes/Enemies/BossFourWay.tscn").instantiate()
 boss.BossPatterns = true
 boss.AttackSeed = 1042
 boss.FireCooldownSeconds = .25
 boss.MoveSpeed = 0
 boss.position = Vector2(512,288)
 arena.add_child(boss)
 var seen := {}
 var volleys := []
 for i in range(1700):
  await frames(1)
  shots = get_nodes_in_group("projectiles")
  if shots.is_empty(): continue
  var pattern: String = boss.GetLastPattern()
  seen[pattern] = true
  volleys.append(pattern)
  if pattern == "Cross":
   verify(shots.size() == 4, "Cross has four projectiles")
   for shot in shots:
    var d = Vector2.RIGHT.rotated(shot.rotation)
    verify(absf(d.x) < .01 or absf(d.y) < .01, "Cross is cardinal")
  elif pattern == "X":
   verify(shots.size() == 4, "X has four projectiles")
   for shot in shots:
    var d = Vector2.RIGHT.rotated(shot.rotation)
    verify(absf(absf(d.x) - absf(d.y)) < .01, "X is diagonal")
  else:
   verify(shots.size() >= 15 and shots.size() <= 18, "Rare ring surrounds the boss")
   for shot in shots:
    verify(absf(shot.rotation) >= deg_to_rad(27)-.01, "Ring preserves an escape opening toward the target")
  for shot in shots: shot.queue_free()
 verify(seen.size() == 3, "Warden executes cross, X and ring")
 verify(volleys.count("Ring") > 0 and volleys.count("Ring") < volleys.size() * .3, "Random ring is rarer than cross/X")
 verify(volleys.count("Cross") > 20 and volleys.count("X") > 20, "Both common patterns remain frequent")
 verify(volleys.slice(0,6) != volleys.slice(6,12), "Warden does not repeat a fixed cycle")
 boss.queue_free()
 await frames(3)
 boss = load("res://Scenes/Enemies/BossMelee.tscn").instantiate()
 boss.position = Vector2(450,288)
 boss.MoveSpeed = 0
 arena.add_child(boss)
 for i in range(320):
  if boss.GetChargeStage() == "Windup": break
  await frames(1)
 verify(boss.GetChargeStage() == "Windup", "Melee boss starts visible windup")
 var origin: Vector2 = boss.position
 var aim: Vector2 = boss.GetChargeAim()
 target.position = Vector2(450,450)
 await frames(25)
 verify(boss.position.distance_to(origin) < .1, "Melee boss stands still during charge preparation")
 for i in range(80):
  if boss.GetChargeStage() == "Charge": break
  await frames(1)
 await frames(12)
 var displacement: Vector2 = boss.position-origin
 verify(displacement.length() > 50 and displacement.normalized().dot(aim) > .999, "Melee charge stays on locked straight line after target dodges")
 await frames(50)
 verify(boss.GetChargeStage() == "Recover", "Melee charge has recovery and cannot chase forever")
 boss.queue_free()
 await frames(3)
 print("ENEMY PATTERN CHECKS: ",passed," passed; ",failed," failed")
 await preload("res://Tests/AudioTestCleanup.gd").drain(self)
 quit(1 if failed else 0)
