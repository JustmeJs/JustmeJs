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
 var kinds := {"Start":0, "Normal":0, "Treasure":0, "Boss":0, "Shop":0}
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
  if room.kind in ["Boss", "Treasure", "Shop"] and room.neighbors.size() != 1: return false
  for d in room.neighbors:
   var target: String = room.neighbors[d]
   if not indexed.has(target): return false
   if indexed[target].position != room.position + offsets[d]: return false
   if indexed[target].neighbors.get(int(d) ^ 1, "") != room.id: return false
   axes[d] = true
   edge_count += 1
 if kinds != {"Start":1, "Normal":8, "Treasure":1, "Boss":1, "Shop":1} or layouts.size() != 6 or axes.size() != 4: return false
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
 if snapshot.any(func(r): return r.theme != ("crypt" if zone <= 3 else "castle") or r.floor != zone): return false
 if zone == 6: return scene == "res://Scenes/Enemies/CastleBoss.tscn"
 return scene in ["res://Scenes/Enemies/BossMelee.tscn", "res://Scenes/Enemies/BossRanged.tscn", "res://Scenes/Enemies/Boss.tscn", "res://Scenes/Enemies/BossFourWay.tscn"]

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
 var sprite = pickup.get_node("Visual/Sprite")
 verify(abs(sprite.texture.get_width() * sprite.scale.x - 40) < 3, "Every passive icon has a consistent visible size")
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
  for zone in range(1, 7):
   var floor_data: Array = manager.PreviewFloor(seed, zone)
   verify(validate_floor(floor_data, zone), "Seed %d zone %d: connected, varied tree with valid special rooms" % [seed, zone])
   signatures[signature(floor_data)] = true
 verify(signatures.size() > 1180, "Different seeds produce diverse room maps and layout choices")
 verify(signature(manager.PreviewFloor(42, 1)) == signature(manager.PreviewFloor(42, 1)), "Same seed reproduces the same floor")
 await physical_doors()
 manager.SeedOverride = 1042
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(4)
 protect_player()
 verify(manager.GetZoneIndex() == 1 and manager.GetFloorCount() == 6, "Run starts on floor one of six")
 var discovered: Array = manager.GetMapSnapshot()
 verify(discovered.size() < 12, "Minimap hides undiscovered rooms")
 verify(discovered.filter(func(r): return r.visited).size() == 1, "Only starting room is initially visited")
 var bosses := []
 var initial_player = player
 for floor_number in range(1, 7):
  var expected_theme := "crypt" if floor_number <= 3 else "castle"
  verify(manager.GetTheme() == expected_theme, "Three crypt floors followed by three castle floors")
  verify(manager.GetCurrentRoom().get_node("RoomArt").Castle == (floor_number >= 4), "Rendered theme follows floor progression")
  var snapshot: Array = manager.GetFloorSnapshot()
  verify(snapshot.filter(func(r): return r.kind == "Treasure").size() == 1, "Exactly one passive treasure per floor")
  await visit_floor()
  verify(collected_icons.size() == floor_number, "Each floor gives exactly one different passive")
  bosses.append(graph[manager.GetCurrentRoomId()].boss_scene)
  if floor_number == 6:
   var final_boss = manager.GetCurrentRoom().get_node("Enemies").get_child(0)
   verify(final_boss.has_method("GetAttackPattern"), "Last floor spawns the unique multi-attack final boss")
   verify(bosses.slice(0, 5).all(func(scene): return scene != bosses[5]), "Final boss differs from every earlier boss")
  await clear_room()
  if floor_number == 6: break
  verify(not paused and not manager.GetRunFinished(), "Intermediate boss never ends the run")
  verify(manager.GetPortalAvailable(), "Every intermediate boss guarantees a portal")
  var boss_id: String = manager.GetCurrentRoomId()
  var portal = manager.GetCurrentRoom().get_node_or_null("ZonePortal")
  verify(portal != null and portal.DestinationFloor == floor_number + 1, "Portal points to the next floor")
  var adjacent: String = graph[boss_id].neighbors.values()[0]
  await navigate(adjacent)
  await navigate(boss_id)
  verify(manager.GetCurrentRoom().get_node("ZonePortal") == portal, "Portal persists after revisiting the boss room")
  verify(manager.GetCurrentRoom().GetAliveCount() == 0, "Boss does not respawn")
  var saved_stats := player_stats()
  var old_rooms := visited_instances.values()
  verify(portal.has_node("CollisionShape2D"), "Portal has a real trigger volume")
  player.position = Vector2(512, 450)
  await frames(2)
  player.position = portal.global_position
  await frames(5)
  verify(manager.GetZoneIndex() == floor_number + 1, "Portal physically enters the next floor exactly once")
  verify(player == initial_player and saved_stats == player_stats(), "Player, health and all passive stats survive transition")
  for old in old_rooms: verify(not is_instance_valid(old), "Previous floor releases cached rooms")
  var prefix := "z%d:" % (floor_number + 1)
  verify(manager.GetMapSnapshot().all(func(r): return r.id.begins_with(prefix)), "Minimap resets to the new floor")
  visited_instances.clear()
 verify(collected_icons.size() == 6, "Six floors give six unique passives")
 verify(is_equal_approx(player.ProjectileSpeed, 375), "Arcane Lens applies its projectile speed improvement once")
 verify(paused and manager.GetRunFinished(), "Only the sixth boss completes the run")
 verify(not manager.GetPortalAvailable() and not manager.GetCurrentRoom().has_node("ZonePortal"), "Final boss ends with victory instead of an undefined floor")
 verify(not manager.RequestNextZone(manager.GetCurrentRoomId()), "Finished run rejects another transition")
 verify(transitions.size() == 4, "Full run traversed all four directions")
 var victory = root.get_node("VictoryScreen")
 victory.get_node("CenterContainer/VBoxContainer/RetryButton").emit_signal("pressed")
 await frames(5)
 verify(manager.GetZoneIndex() == 1 and not manager.GetRunFinished(), "Retry creates a fresh first floor")
 player = get_first_node_in_group("player")
 verify(player.GetMaxHealthHalfHearts() == 8 and is_equal_approx(player.MoveSpeed, 150)
  and is_equal_approx(player.ProjectileSpeed, 300) and player.GetCoins() == 0, "Retry resets passive upgrades")
 protect_player()
 graph = index_graph(manager.GetFloorSnapshot())
 visited_instances.clear()
 var retry_boss: String
 for room in graph.values():
  if room.kind == "Boss": retry_boss = room.id
 await navigate(retry_boss)
 await clear_room()
 verify(not paused and not manager.GetRunFinished() and manager.GetPortalAvailable(), "Guaranteed portal also works after retry")
 print("ROGUELIKE CHECKS: ", passed, " passed; ", failed, " failed")
 await preload("res://Tests/AudioTestCleanup.gd").drain(self)
 quit(1 if failed > 0 else 0)

func player_stats() -> Array:
 return [player.GetCurrentHealthHalfHearts(), player.GetMaxHealthHalfHearts(), player.MoveSpeed,
  player.BaseDamage, player.FireCooldownSeconds, player.ProjectileSpeed, player.GetCoins()]
