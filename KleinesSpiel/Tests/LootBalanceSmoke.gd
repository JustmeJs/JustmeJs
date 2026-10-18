extends "res://Tests/RoguelikeSmoke.gd"
var inspected_kills := 0
var observed_drops := 0
var floor_income := 0

func clear_room() -> void:
 var room = manager.GetCurrentRoom()
 var expected := -1
 if room.GetRoomKind() == "Normal" and not room.IsCleared():
  expected = room.get_children().filter(func(n): return n.is_in_group("coins")).size()
  for enemy in room.get_node("Enemies").get_children():
   if enemy.GetCurrentHealth() <= 0: continue
   verify(enemy.CoinDropValue in [0,1], "Normal drop is either absent or a single coin")
   inspected_kills += 1
   observed_drops += enemy.CoinDropValue
   floor_income += enemy.CoinDropValue
   expected += enemy.CoinDropValue
 await super.clear_room()
 if expected >= 0:
  var actual = room.get_children().filter(func(n): return n.is_in_group("coins")).size()
  verify(actual == expected, "Configured loot becomes actual pickups after kills")

func run() -> void:
 manager = root.get_node("DungeonManager")
 var budgets := {}
 for level in [1,3,6]:
  var income := 0
  for seed in range(1,31):
   manager.SeedOverride = seed
   change_scene_to_file("res://Scenes/Main.tscn")
   await frames(4)
   protect_player()
   if level != 1:
    manager.call("BeginZone", level)
    await frames(4)
   graph = index_graph(manager.GetFloorSnapshot())
   visited_instances.clear()
   floor_income = 0
   for entry in graph.values():
    if entry.kind != "Normal": continue
    await navigate(entry.id)
    await clear_room()
   income += floor_income
  budgets[level] = income / 30.0
 var ratio := float(observed_drops) / inspected_kills
 verify(inspected_kills > 1500 and abs(ratio - 0.35) < 0.04, "Actual spawned loot approaches the configured 35% rate over many seeds")
 verify(budgets[1] + 3 < 15 and budgets[1] > 3, "Typical first floor funds healing but requires saving for a passive")
 print("LOOT SAMPLE: ", inspected_kills, " inspected enemies; ", observed_drops, " drops; ratio ", ratio)
 print("NORMAL-ROOM INCOME PER FLOOR (30 seeds each): ", budgets)
 print("LOOT BALANCE CHECKS: ", passed, " passed; ", failed, " failed")
 await preload("res://Tests/AudioTestCleanup.gd").drain(self)
 quit(1 if failed else 0)
