extends SceneTree
# Tests real playback, input cadence, range, directional frames and shuffled runs.
var passed := 0
var failed := 0
func verify(ok: bool, description: String):
 if ok: passed += 1
 else:
  failed += 1
  push_error(description)
func frames(n: int):
 for i in range(n): await process_frame
func _initialize(): call_deferred("run")
func voice_for(audio, effect: String):
 for child in audio.get_children():
  if child is AudioStreamPlayer and child.stream != null and child.stream.resource_path.ends_with("/" + effect + ".wav"):
   return child
 return null
func run():
 var audio = root.get_node("GameAudio")
 var first_crypt := {}
 var first_castle := {}
 for seed in range(1,301):
  var tracks = audio.PreviewMusic(seed)
  var unique := {}
  for i in range(6):
   unique[tracks[i]] = true
   verify(tracks[i].begins_with("crypt_" if i < 3 else "castle_"), "Playlist follows floor theme")
  verify(unique.size() == 6, "Six floors never repeat exploration music")
  verify(tracks == audio.PreviewMusic(seed), "Playlist stays stable within a seeded run")
  first_crypt[tracks[0]] = true
  first_castle[tracks[3]] = true
 verify(first_crypt.size() == 3 and first_castle.size() == 3, "All three tracks can be selected first in each zone")
 verify(ProjectSettings.get_setting("application/config/name") == "Magic Loop", "Published game title")
 change_scene_to_file("res://Scenes/Main.tscn")
 await frames(4)
 var player = get_first_node_in_group("player")
 var tutorial: String = root.get_node("DungeonManager").GetCurrentRoom().get_node("TutorialLabel").text
 verify(not tutorial.to_lower().contains("tab") and not tutorial.to_lower().contains("green coin"), "Requested hints removed")
 # Every real keyboard cast is counted even after its projectile expires.
 var spawned := []
 var observer = func(n):
  if n is Area2D and n.collision_layer == 8: spawned.append(Engine.get_physics_frames())
 node_added.connect(observer)
 Input.action_press("aim_right")
 await frames(120)
 Input.action_release("aim_right")
 node_added.disconnect(observer)
 verify(spawned.size() == 5, "Wand fires five times across two seconds")
 for i in range(1,spawned.size()): verify(spawned[i]-spawned[i-1] >= 28, "Wand respects slower .48-second cadence")
 var cast_voice = voice_for(audio,"cast")
 verify(cast_voice != null and cast_voice.volume_db <= -24 and cast_voice.stream.get_length() < .25, "Soft, short wand sound")
 for shot in get_nodes_in_group("projectiles"): shot.queue_free()
 await frames(2)
 var coin = load("res://Scripts/Items/CoinPickup.cs").new()
 coin.position = player.position
 current_scene.add_child(coin)
 var before: int = player.GetCoins()
 await frames(4)
 verify(player.GetCoins() == before + 1 and not is_instance_valid(coin), "Real coin collision collects once")
 verify(voice_for(audio,"coin") != null, "Coin pickup plays its own sound")
 # Isolated arena, so expiration cannot be caused by room walls.
 current_scene.queue_free()
 await frames(3)
 var arena := Node2D.new()
 root.add_child(arena)
 current_scene = arena
 for speed in [300.0,375.0,1800.0]:
  var shot = load("res://Scenes/Projectiles/PlayerProjectile.tscn").instantiate()
  arena.add_child(shot)
  shot.position = Vector2(2000,2000)
  shot.Launch(Vector2.RIGHT,speed,1.0)
  var end := [Vector2.ZERO]
  shot.tree_exiting.connect(func(): end[0] = shot.position)
  var steps := 0
  while is_instance_valid(shot) and steps < 180:
   await frames(1)
   steps += 1
  verify(not is_instance_valid(shot), "Projectile expires at finite range")
  verify(absf(end[0].distance_to(Vector2(2000,2000))-220.0)<.01, "Range stays exactly 220 at every speed")
 var zombie = load("res://Scenes/Enemies/Zombie.tscn").instantiate()
 zombie.MoveSpeed = 0
 arena.add_child(zombie)
 await frames(130)
 verify(voice_for(audio,"zombie") != null, "Living zombie emits a groan")
 zombie.TakeDamage(100)
 await frames(40)
 verify(not is_instance_valid(zombie), "Dead zombie stops processing and is removed")
 # All drawn characters expose actual distinct walk/cast cells in four directions.
 for role in ["crypt_imp","crypt_archer","crypt_watcher","crypt_ogre","crypt_guardian","crypt_summoner","crypt_zombie","castle_archer","castle_knight","castle_mage","castle_warden","castle_summoner","castle_zombie"]:
  var sf: SpriteFrames = load("res://Resources/Animations/Directions/" + role + ".tres")
  var visual = load("res://Scripts/Common/ActorAnimator.cs").new()
  visual.MirrorLeft = true
  var sprite := AnimatedSprite2D.new()
  sprite.name = "Sprite"
  sprite.sprite_frames = sf
  visual.add_child(sprite)
  arena.add_child(visual)
  for direction in {"down":Vector2.DOWN,"up":Vector2.UP,"left":Vector2.LEFT,"right":Vector2.RIGHT}:
   var d: Vector2 = {"down":Vector2.DOWN,"up":Vector2.UP,"left":Vector2.LEFT,"right":Vector2.RIGHT}[direction]
   await frames(16)
   visual.SetMotion(d*100,d)
   await frames(2)
   verify(sprite.animation == StringName("walk_"+direction), role + " walks in " + direction)
   verify(sf.get_frame_texture("walk_"+direction,0).region != sf.get_frame_texture("walk_"+direction,1).region, role + " uses different drawn walk poses")
   visual.PlayAttack(d)
   await frames(1)
   verify(sprite.animation == StringName("attack_"+direction), role + " attacks in " + direction)
   verify(sprite.flip_h == (direction == "left"), role + " mirrors only the left profile")
   verify(sf.get_frame_texture("attack_"+direction,0).region != sf.get_frame_texture("attack_"+direction,1).region, role + " has distinct windup and release")
  visual.queue_free()
 # Observe complete Regent schedulers with different seeds, including transitions.
 var target := Node2D.new()
 target.position = Vector2(650,288)
 target.add_to_group("player")
 arena.add_child(target)
 var first_patterns := {}
 for seed in [1,2,3,4,5,6]:
  var boss = load("res://Scenes/Enemies/CastleBoss.tscn").instantiate()
  boss.AttackSeed = seed
  boss.position = Vector2(512,288)
  arena.add_child(boss)
  var sequence := []
  var previous_stage := ""
  for i in range(2600):
   await frames(1)
   var stage: String = boss.GetAttackStage()
   if stage == "Telegraph" and previous_stage != stage:
    sequence.append(boss.GetAttackPattern())
    if sequence.size() >= 10: break
   previous_stage = stage
   for shot in get_nodes_in_group("projectiles"): shot.queue_free()
  verify(sequence.size() == 10, "Seeded boss completes ten random decisions")
  first_patterns[sequence[0]] = true
  for i in range(1,sequence.size()): verify(sequence[i] != sequence[i-1], "Regent avoids immediate identical attack")
  var successors := {}
  for i in range(1,sequence.size()): successors[str(sequence[i-1])+":"+str(sequence[i])] = true
  verify(successors.size() > 3, "Regent has branching choices, not a fixed three-attack cycle")
  boss.TakeDamage(10000)
  await frames(40)
 verify(first_patterns.size() > 1, "Regent can open with different attacks")
 print("MAGIC LOOP CHECKS: ",passed," passed; ",failed," failed")
 await preload("res://Tests/AudioTestCleanup.gd").drain(self)
 quit(1 if failed else 0)
