extends SceneTree
# Run with Godot .NET: --headless --fixed-fps 60 --path . --script res://Tests/RoguelikeSmoke.gd
var passed := 0
var failed := 0
var manager: Node
var player: Node2D
var graph := {}
var visited_instances := {}
var collected_icons := {}
var transitions := {}

func verify(ok: bool, description: String) -> void:
 if ok: passed += 1
 else:
  failed += 1
  push_error(description)
func frames(count: int) -> void:
 for i in range(count): await process_frame
func _initialize() -> void:
 call_deferred("run")
func index_graph(snapshot: Array) -> Dictionary:
 var result := {}
 for room in snapshot: result[room.id] = room
 return result
func signature(snapshot: Array) -> String:
 var parts: Array[String] = []
 for room in snapshot:
  parts.append(str(room.position) + ":" + str(room.kind) + ":" + str(room.layout) + ":" + str(room.boss_scene))
 return ";".join(parts)
func validate_floor(snapshot: Array, zone: int) -> bool:
 if snapshot.size() != 12: return false
 var indexed := index_graph(snapshot)
 var kinds := {"Start":0, "Normal":0, "Treasure":0, "Boss":0}
 var positions := {}
 var layouts := {}
 var start := ""
 var edge_count := 0
 var axes := {}
 var boss := ""
 var offsets := [Vector2i.UP, Vector2i.DOWN, Vector2i.LEFT, Vector2i.RIGHT]
 for room in snapshot:
  kinds[room.kind] += 1
  if positions.has(room.position): return false
  positions[room.position] = true
  if abs(room.position.x) > 2 or abs(room.position.y) > 2: return false
  if room.kind == "Start": start = room.id
  if room.kind == "Boss": boss = room.id
  if room.kind == "Normal": layouts[room.layout] = true
  if room.kind in ["Boss", "Treasure"] and room.neighbors.size() != 1: return false
  for d in room.neighbors:
   var target: String = room.neighbors[d]
   if not indexed.has(target): return false
   if indexed[target].position != room.position + offsets[d]: return false
   if indexed[target].neighbors.get(int(d) ^ 1, "") != room.id: return false
   axes[d] = true
   edge_count += 1
 if kinds != {"Start":1, "Normal":8, "Treasure":2, "Boss":1} or layouts.size() != 6 or axes.size() != 4: return false
 if edge_count != 22 or indexed[start].neighbors.size() < 2: return false
 var distances := {start:0}
 var queue := [start]
 while not queue.is_empty():
  var id: String = queue.pop_front()
  for next in indexed[id].neighbors.values():
   if not distances.has(next):
    distances[next] = distances[id] + 1
    queue.append(next)
 if distances.size() != 12: return false
 for room in snapshot:
  if room.neighbors.size() == 1 and distances[room.id] > distances[boss]: return false
 var scene: String = indexed[boss].boss_scene
 return scene in (["res://Scenes/Enemies/BossMelee.tscn", "res://Scenes/Enemies/BossRanged.tscn"] if zone == 1 else ["res://Scenes/Enemies/Boss.tscn", "res://Scenes/Enemies/BossFourWay.tscn"])

func protect_player() -> void:
 player = get_first_node_in_group("player")
 player.HurtInvincibilitySeconds = 1000
 player.TakeDamage(1)
 player.set_physics_process(false)
func clear_room() -> void:
 var room = manager.GetCurrentRoom()
 if room.GetAliveCount() > 0:
  var first_direction: int = graph[room.GetRoomId()].neighbors.keys()[0]
  verify(not manager.Travel(first_direction), "Combat blocks travel")
  for enemy in room.get_node("Enemies").get_children():
   if enemy.has_method("TakeDamage"): enemy.TakeDamage(10000)
 await frames(3)
 verify(room.GetAliveCount() == 0 and room.IsCleared(), "Clearing enemies unlocks room")
 verify(get_nodes_in_group("enemies").is_empty(), "No defeated or cached enemy remains active")
 for door in room.get_node("Doors").get_children():
  verify(door.GetIsOpen(), "Cleared room opens each door")

func path_to(target: String) -> Array:
 var start: String = manager.GetCurrentRoomId()
 var queue := [start]
 var previous := {start: null}
 while not queue.is_empty():
  var id: String = queue.pop_front()
  if id == target: break
  for next in graph[id].neighbors.values():
   if not previous.has(next):
    previous[next] = id
    queue.append(next)
 var result := []
 var at: String = target
 while at != start:
  result.push_front(at)
  at = previous[at]
 return result
func navigate(target: String) -> void:
 for next in path_to(target):
  await clear_room()
  var source: String = manager.GetCurrentRoomId()
  var direction := -1
  for d in graph[source].neighbors:
   if graph[source].neighbors[d] == next: direction = d
  var room = manager.GetCurrentRoom()
  visited_instances[source] = room
  var old_shots := get_nodes_in_group("projectiles")
  var door: Node
  for candidate in room.get_node("Doors").get_children():
   if candidate.DoorDirection == direction: door = candidate
  verify(door != null, "Graph connection has a physical door")
  door.get_node("TriggerArea").emit_signal("body_entered", player)
  door.get_node("TriggerArea").emit_signal("body_entered", player)
  await frames(4)
  verify(manager.GetCurrentRoomId() == next, "Door follows reciprocal graph edge exactly once")
  transitions[direction] = true
  for shot in old_shots: verify(not is_instance_valid(shot), "Old projectiles are removed on travel")
  var query := PhysicsPointQueryParameters2D.new()
  query.position = player.global_position
  query.collision_mask = 1
  verify(root.world_2d.direct_space_state.intersect_point(query).is_empty(), "Arrival position is not inside a wall or obstacle")
  if visited_instances.has(next):
   verify(manager.GetCurrentRoom() == visited_instances[next], "Revisits reuse the same room instance")
   verify(manager.GetCurrentRoom().GetAliveCount() == 0, "Cleared rooms do not respawn enemies")

func collect_treasure() -> void:
 var room = manager.GetCurrentRoom()
 var pickup = room.get_node_or_null("TreasureItem")
 if pickup == null: return
 var data = pickup.Data
 verify(not collected_icons.has(data.Icon.resource_path), "Treasure bag gives a different passive icon")
 collected_icons[data.Icon.resource_path] = true
 verify(pickup.get_node("Visual/Sprite").texture == data.Icon, "Passive shows its own buff icon")
 player.position = pickup.global_position
 await frames(3)
 pickup.emit_signal("body_entered", player)
 pickup.emit_signal("body_entered", player)
 await frames(21)
 verify(not room.has_node("TreasureItem"), "Collected treasure finishes animation and is removed")

func visit_floor() -> void:
 graph = index_graph(manager.GetFloorSnapshot())
 var boss: String
 for room in graph.values():
  if room.kind == "Boss": boss = room.id
  else:
   await navigate(room.id)
   await clear_room()
   if room.kind == "Treasure": await collect_treasure()
 # Revisit every treasure after collection; no new item may be spawned.
 for room in graph.values():
  if room.kind == "Treasure":
   await navigate(room.id)
   verify(not manager.GetCurrentRoom().has_node("TreasureItem"), "Treasure stays collected after revisiting")
 await navigate(boss)

func walk_through(door: Node, direction: int) -> void:
 var offset: Vector2 = [Vector2.UP, Vector2.DOWN, Vector2.LEFT, Vector2.RIGHT][direction]
 var action: String = ["move_up", "move_down", "move_left", "move_right"][direction]
 verify(door.get_node("Blocker/CollisionShape2D").disabled, "Open gate disables its physical blocker")
 player.position = door.global_position - offset * 50
 var before: String = manager.GetCurrentRoomId()
 var expected: String = door.TargetRoomId
 Input.action_press(action)
 await frames(18)
 Input.action_release(action)
 await frames(3)
 verify(manager.GetCurrentRoomId() == expected and before != expected, "Keyboard movement actually crosses a door")

func physical_doors() -> void:
 manager.SeedOverride = 7
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(4)
 protect_player()
 player.set_physics_process(true)
 graph = index_graph(manager.GetFloorSnapshot())
 var start: String = manager.GetCurrentRoomId()
 var physical_axes := {}
 for d in graph[start].neighbors:
  if manager.GetCurrentRoomId() != start:
   verify(false, "Physical traversal returns to the starting room")
   return
  var door = manager.GetCurrentRoom().get_node("Doors").get_children().filter(func(n): return n.DoorDirection == d)[0]
  await walk_through(door, d)
  physical_axes[d] = true
  if manager.GetCurrentRoom().GetAliveCount() > 0:
   var back_direction: int = int(d) ^ 1
   var closed = manager.GetCurrentRoom().get_node("Doors").get_children().filter(func(n): return n.DoorDirection == back_direction)[0]
   var axis: Vector2 = [Vector2.UP, Vector2.DOWN, Vector2.LEFT, Vector2.RIGHT][back_direction]
   var action: String = ["move_up", "move_down", "move_left", "move_right"][back_direction]
   var blocked_id: String = manager.GetCurrentRoomId()
   verify(not closed.get_node("Blocker/CollisionShape2D").disabled, "Combat gate has an active collision blocker")
   player.position = closed.global_position - axis * 50
   Input.action_press(action)
   await frames(18)
   Input.action_release(action)
   await frames(3)
   verify(manager.GetCurrentRoomId() == blocked_id, "Closed combat gate prevents a physical exit")
   verify((closed.global_position - player.position).dot(axis) >= 27.5, "Player cannot walk through the closed gate")
  await clear_room()
  var opposite: int = int(d) ^ 1
  var backs = manager.GetCurrentRoom().get_node("Doors").get_children().filter(func(n): return n.DoorDirection == opposite)
  if backs.is_empty():
   verify(false, "Arrival room has reciprocal return door")
   return
  var back = backs[0]
  await walk_through(back, opposite)
  physical_axes[opposite] = true
 verify(physical_axes.size() == 4 and manager.GetCurrentRoomId() == start, "Real collision/trigger traversal works in all four directions")
 player.set_physics_process(false)

func run() -> void:
 manager = root.get_node("DungeonManager")
 var signatures := {}
 for seed in range(1, 201):
  for zone in [1, 2]:
   var floor_data: Array = manager.PreviewFloor(seed, zone)
   verify(validate_floor(floor_data, zone), "Seed %d zone %d: connected, varied tree with valid special rooms" % [seed, zone])
   signatures[signature(floor_data)] = true
 verify(signatures.size() > 390, "Different seeds produce diverse room maps and layout choices")
 verify(signature(manager.PreviewFloor(42, 1)) == signature(manager.PreviewFloor(42, 1)), "Same seed reproduces the same floor")
 await physical_doors()
 manager.NextZoneChance = 1.0
 manager.SeedOverride = 1042
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(4)
 protect_player()
 verify(manager.GetZoneIndex() == 1, "Run starts in first zone")
 var discovered: Array = manager.GetMapSnapshot()
 verify(discovered.size() < 12, "Minimap hides undiscovered rooms")
 verify(discovered.filter(func(r): return r.visited).size() == 1, "Only starting room is initially visited")
 await visit_floor()
 var first_boss: String = graph[manager.GetCurrentRoomId()].boss_scene
 var first_room_instances := visited_instances.values()
 await clear_room()
 verify(not paused and not manager.GetRunFinished(), "First boss with portal chance one does not end run")
 verify(manager.GetPortalAvailable(), "First boss rolls a portal")
 var boss_id: String = manager.GetCurrentRoomId()
 var portal = manager.GetCurrentRoom().get_node_or_null("ZonePortal")
 verify(portal != null, "Boss victory spawns a real portal")
 # Backtrack and return: no reroll and no duplicate portal or boss.
 var adjacent: String = graph[boss_id].neighbors.values()[0]
 await navigate(adjacent)
 await navigate(boss_id)
 verify(manager.GetCurrentRoom().get_node("ZonePortal") == portal, "Portal and its outcome persist after revisiting boss")
 verify(manager.GetCurrentRoom().GetAliveCount() == 0, "Boss does not respawn")
 var saved_stats = [player.GetCurrentHealthHalfHearts(), player.GetMaxHealthHalfHearts(), player.MoveSpeed, player.BaseDamage, player.FireCooldownSeconds]
 var first_player = player
 verify(portal.has_node("CollisionShape2D"), "Portal has a real trigger volume")
 # Arm the portal outside its trigger before entering it.
 player.position = Vector2(512, 450)
 await frames(2)
 player.position = portal.global_position
 await frames(5)
 verify(manager.GetZoneIndex() == 2, "Portal enters second zone once")
 verify(player == first_player, "Zone transition preserves player instance")
 verify(saved_stats == [player.GetCurrentHealthHalfHearts(), player.GetMaxHealthHalfHearts(), player.MoveSpeed, player.BaseDamage, player.FireCooldownSeconds], "Health and passive buffs persist between zones")
 for old in first_room_instances: verify(not is_instance_valid(old), "Old zone releases detached cached rooms")
 verify(manager.GetMapSnapshot().all(func(r): return r.id.begins_with("z2:")), "Minimap resets to the second zone")
 verify(manager.GetCurrentRoom().get_node("RoomArt").Amethyst, "Second zone uses its own visual theme")
 visited_instances.clear()
 await visit_floor()
 verify(first_boss != graph[manager.GetCurrentRoomId()].boss_scene, "Final zone has a different boss")
 verify(collected_icons.size() == 4, "Four treasure rooms provide four distinct upgrades")
 await clear_room()
 verify(paused and manager.GetRunFinished(), "Second boss completes the run")
 verify(not manager.GetPortalAvailable(), "Final boss does not lead to an undefined third zone")
 verify(not manager.RequestNextZone(manager.GetCurrentRoomId()), "Finished run rejects another zone transition")
 verify(transitions.size() == 4, "Full run traversed up, down, left and right")
 # Retry from the real UI with a no-portal roll to exercise the other ending.
 manager.NextZoneChance = 0.0
 var victory = root.get_node("VictoryScreen")
 victory.get_node("CenterContainer/VBoxContainer/RetryButton").emit_signal("pressed")
 await frames(5)
 verify(manager.GetZoneIndex() == 1 and not manager.GetRunFinished(), "Retry creates a fresh first zone")
 player = get_first_node_in_group("player")
 verify(player.GetMaxHealthHalfHearts() == 8 and is_equal_approx(player.MoveSpeed, 150), "Retry resets all passive upgrades")
 protect_player()
 graph = index_graph(manager.GetFloorSnapshot())
 visited_instances.clear()
 var retry_boss: String
 for room in graph.values():
  if room.kind == "Boss": retry_boss = room.id
 await navigate(retry_boss)
 await clear_room()
 verify(paused and manager.GetRunFinished() and manager.GetZoneIndex() == 1, "Zero portal chance ends first-zone run without a dead end")
 verify(not manager.GetPortalAvailable() and not manager.GetCurrentRoom().has_node("ZonePortal"), "No portal appears when its roll fails")
 print("ROGUELIKE CHECKS: ", passed, " passed; ", failed, " failed")
 quit(1 if failed > 0 else 0)
