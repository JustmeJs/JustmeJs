extends "res://Tests/RoguelikeSmoke.gd"
# Real input, floor transitions, spawned enemies and audio selection.
func key_event(code: Key, down: bool) -> void:
 var e := InputEventKey.new()
 e.keycode = code
 e.physical_keycode = code
 e.pressed = down
 Input.parse_input_event(e)

func run() -> void:
 manager = root.get_node("DungeonManager")
 manager.SeedOverride = 1042
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(4)
 player = get_first_node_in_group("player")
 var audio = root.get_node("GameAudio")
 verify(player.position.distance_to(Vector2(512,288)) < 1, "Run starts in room center")
 verify(player.motion_mode == CharacterBody2D.MOTION_MODE_FLOATING, "Top-down controller uses floating collisions")
 for pair in [[KEY_D,KEY_S,1,1],[KEY_D,KEY_W,1,-1],[KEY_A,KEY_S,-1,1],[KEY_A,KEY_W,-1,-1]]:
  player.position = Vector2(512,288)
  key_event(pair[0],true)
  key_event(pair[1],true)
  key_event(KEY_RIGHT,true)
  await frames(12)
  var moved: Vector2 = player.position - Vector2(512,288)
  verify(moved.x * pair[2] > 10 and moved.y * pair[3] > 10, "Physical diagonal movement with shooting: " + str(pair))
  verify(absf(moved.x) - absf(moved.y) < 0.5 and moved.length() < 34, "Diagonal has normalized speed")
  key_event(pair[0],false)
  key_event(pair[1],false)
  key_event(KEY_RIGHT,false)
  await frames(2)
 # Sliding down the right wall is the reported blocked-direction case.
 player.position = Vector2(723,200)
 key_event(KEY_D,true)
 key_event(KEY_S,true)
 await frames(20)
 verify(player.position.y > 230 and player.position.x < 728, "Right + down slides down right wall")
 key_event(KEY_D,false)
 key_event(KEY_S,false)
 player.position = Vector2(512,288)
 var clock: float = manager.GetRunSeconds()
 await frames(60)
 verify(manager.GetRunSeconds() > clock + .9, "Run timer advances during gameplay")
 paused = true
 clock = manager.GetRunSeconds()
 key_event(KEY_TAB,true)
 await frames(30)
 var hud = get_first_node_in_group("hud")
 verify(is_equal_approx(manager.GetRunSeconds(),clock), "Pause freezes run time")
 verify(hud.get_node("RunTimeLabel").visible, "Tab can reveal the clock even while paused")
 verify(audio.IsMusicPaused(), "Pause freezes soundtrack")
 key_event(KEY_TAB,false)
 await frames(2)
 verify(not hud.get_node("RunTimeLabel").visible, "Releasing Tab hides run time")
 paused = false
 await frames(2)
 verify(not audio.IsMusicPaused() and audio.get_node("Music").playing, "Music resumes after pause")
 verify(not hud.has_node("HealthLabel") and hud.has_node("HealthIcons"), "Only heart icons display health")
 verify(not manager.GetCurrentRoom().get_node("TutorialLabel").text.contains("treasure"), "Tutorial no longer sets treasure as objective")
 var heart = load("res://Scenes/Items/HeartPickup.tscn").instantiate()
 current_scene.add_child(heart)
 var hs = heart.get_node("Visual/Sprite")
 verify(hs.texture.get_width() * hs.scale.x < 23, "Dropped heart smaller than player")
 verify(heart.get_node("CollisionShape2D").shape.size == Vector2(16,16), "Heart pickup collider matches smaller art")
 heart.queue_free()
 protect_player()
 graph = index_graph(manager.GetFloorSnapshot())
 var saw_summoner := false
 for entry in graph.values():
  if entry.kind != "Normal": continue
  await navigate(entry.id)
  var room = manager.GetCurrentRoom()
  var summons = room.get_node("Enemies").get_children().filter(func(e): return e.get_script().resource_path.ends_with("EnemySummoner.cs") and e.GetCurrentHealth() > 0)
  if summons.is_empty(): continue
  saw_summoner = true
  var summoner = summons[0]
  player.position = Vector2(512,480)
  for e in room.get_node("Enemies").get_children():
   if e != summoner and e.has_method("TakeDamage"): e.TakeDamage(10000)
  var before: int = room.GetAliveCount()
  verify(before == 1 and summoner.ContactDamageHalfHearts == 0, "Only summoner remains; has no contact attack")
  verify(summoner.get_node("Visual/Sprite").sprite_frames.get_frame_texture("idle",0).atlas.resource_path.ends_with("zone_enemies.png"), "Cult summoner uses new hooded art")
  await frames(275)
  verify(get_nodes_in_group("summoned_zombies").is_empty(), "No summon before five seconds")
  paused = true
  await frames(90)
  verify(get_nodes_in_group("summoned_zombies").is_empty(), "Pause freezes summon timer")
  paused = false
  await frames(35)
  var zombies = get_nodes_in_group("summoned_zombies")
  verify(zombies.size() == 1 and room.GetAliveCount() == before+1, "One zombie spawns at five seconds and participates in room clear")
  if not zombies.is_empty():
   var zombie = zombies[0]
   verify(zombie.MoveSpeed > 85*.8 and zombie.CoinDropValue == 0 and zombie.HeartDropChance == 0, "Zombies move faster than melee and cannot farm loot")
   zombie.set_physics_process(false)
  for wave in range(4):
   await frames(301)
   for zombie in get_nodes_in_group("summoned_zombies"): zombie.set_physics_process(false)
  verify(get_nodes_in_group("summoned_zombies").size() == 4, "Summoner respects four living minion cap")
  summoner.TakeDamage(10000)
  await frames(35)
  verify(not room.IsCleared(), "Killing summoner leaves living zombies to defeat")
  for zombie in get_nodes_in_group("summoned_zombies"): zombie.TakeDamage(10000)
  await frames(5)
  verify(room.IsCleared() and room.GetAliveCount() == 0, "Defeating summoned zombies opens doors correctly")
  await frames(310)
  verify(get_nodes_in_group("summoned_zombies").is_empty(), "Dead summoner cannot summon again")
  break
 verify(saw_summoner, "Seed generates a cult summoner in real rooms")
 # Traverse all six floors, checking guaranteed discoverable shops and centered arrivals.
 for floor_number in range(1,7):
  graph = index_graph(manager.GetFloorSnapshot())
  var shop: Dictionary = graph.values().filter(func(r): return r.kind == "Shop")[0]
  verify(manager.GetMapSnapshot().any(func(r): return r.id == shop.id), "Shop visible on minimap floor " + str(floor_number))
  await navigate(shop.id)
  verify(manager.GetCurrentRoom().get_node("ShopStock").get_child_count() == 3, "Real reachable stocked shop on floor " + str(floor_number))
  var shop_heart = manager.GetCurrentRoom().get_node("ShopStock/Offer0/Visual/Sprite")
  verify(shop_heart.texture.get_width() * shop_heart.scale.x < 23, "Shop heart uses smaller art")
  verify(audio.GetCurrentTrack() == ("crypt_rooms" if floor_number < 4 else "castle_rooms"), "Exploration music matches zone")
  var variants := {}
  for entry in graph.values():
   if entry.kind != "Normal": continue
   await navigate(entry.id)
   var room = manager.GetCurrentRoom()
   variants[room.get_node("RoomArt").Design] = true
   for e in room.get_node("Enemies").get_children():
    if not e.has_method("GetCurrentHealth") or e.GetCurrentHealth() <= 0: continue
    verify(is_equal_approx(e.HeartDropChance,.05), "Normal heart chance reduced to five percent")
    if floor_number >= 4:
     var tex = e.get_node("Visual/Sprite").sprite_frames.get_frame_texture("idle",0)
     verify(tex.atlas.resource_path.ends_with("zone_enemies.png"), "Castle enemies use zone-specific art")
   await clear_room()
  if floor_number >= 4: verify(variants.size() == 6, "Castle floor has six room designs")
  var boss_room: Dictionary = graph.values().filter(func(r): return r.kind == "Boss")[0]
  await navigate(boss_room.id)
  verify(audio.GetCurrentTrack() not in ["crypt_rooms","castle_rooms"], "Boss starts a distinct soundtrack")
  var music = audio.get_node("Music")
  verify(music.playing and music.stream.loop, "Boss music plays as seamless loop")
  await clear_room()
  if floor_number == 6: break
  verify(audio.GetCurrentTrack() in ["crypt_rooms","castle_rooms"], "Cleared boss restores exploration music")
  var portal = manager.GetCurrentRoom().get_node("ZonePortal")
  var old_time: float = manager.GetRunSeconds()
  player.position = portal.global_position
  await frames(5)
  verify(manager.GetZoneIndex() == floor_number+1 and player.position.distance_to(Vector2(512,288)) < 1, "Portal arrives at room center on next floor")
  verify(manager.GetCurrentRoom().GetRoomKind() == "Start", "Portal does not accidentally enter adjacent room")
  verify(manager.GetRunSeconds() >= old_time, "Floor transition preserves run clock")
  visited_instances.clear()
 verify(manager.GetRunFinished() and not audio.get_node("Music").playing, "Victory ends music and timer")
 paused = false
 root.get_node("VictoryScreen").queue_free()
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(4)
 verify(manager.GetRunSeconds() < .2, "New run resets timer")
 player = get_first_node_in_group("player")
 player.TakeDamage(10000)
 await frames(3)
 verify(paused and not audio.get_node("Music").playing, "Death stops music")
 var death_voices = audio.get_children().filter(func(n): return n is AudioStreamPlayer and n.stream != null and n.stream.resource_path.ends_with("/death.wav"))
 verify(death_voices.size() == 1 and not death_voices[0].stream_paused and death_voices[0].volume_db <= -18, "Short quiet death sound plays on paused game-over screen")
 var death_time: float = manager.GetRunSeconds()
 await frames(30)
 verify(is_equal_approx(manager.GetRunSeconds(),death_time), "Game over freezes run timer")
 print("EXPANSION CHECKS: ",passed," passed; ",failed," failed")
 await preload("res://Tests/AudioTestCleanup.gd").drain(self)
 quit(1 if failed else 0)
