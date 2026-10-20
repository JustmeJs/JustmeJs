extends "res://Tests/RoguelikeSmoke.gd"
# Exercises destination queries against the actual room collision bodies.
func run():
 manager = root.get_node("DungeonManager")
 manager.SeedOverride = 1042
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(4)
 player = get_first_node_in_group("player")
 protect_player()
 graph = index_graph(manager.GetFloorSnapshot())
 for layout in range(6):
  var entry = graph.values().filter(func(r):return r.kind=="Normal" and r.layout==layout)[0]
  await navigate(entry.id)
  await clear_room()
  var room = manager.GetCurrentRoom()
  player.position = Vector2(512,288)
  var boss = load("res://Scenes/Enemies/BossRanged.tscn").instantiate()
  boss.BlinkSeed = 400+layout
  boss.MoveSpeed = 0
  boss.position = Vector2(340,120)
  room.get_node("Enemies").add_child(boss)
  var previous := 0
  for tick in range(1000):
   await frames(1)
   for shot in get_nodes_in_group("projectiles"): shot.queue_free()
   if boss.GetBlinkTeleports() > previous:
    verify(boss.position.distance_to(player.position)>=160,"Layout "+str(layout)+": safe player distance")
    var q := PhysicsShapeQueryParameters2D.new()
    q.shape = boss.get_node("CollisionShape2D").shape
    q.transform = boss.global_transform
    q.collision_mask = 7
    q.exclude = [boss.get_rid()]
    verify(boss.get_world_2d().direct_space_state.intersect_shape(q).is_empty(),"Layout "+str(layout)+": destination clears actual walls, player and pillars")
    previous = boss.GetBlinkTeleports()
   if boss.GetBlinkSequences() >= 1: break
  verify(previous==3 and boss.GetBlinkShots()==3,"Layout "+str(layout)+": three valid blinks complete")
  boss.TakeDamage(1000)
  await frames(40)
 print("BLINK SAFETY CHECKS: ",passed," passed; ",failed," failed")
 await preload("res://Tests/AudioTestCleanup.gd").drain(self)
 quit(1 if failed else 0)
