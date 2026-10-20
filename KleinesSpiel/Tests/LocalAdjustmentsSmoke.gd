extends SceneTree
var passed := 0
var failed := 0
var had_settings := false
var settings_backup := PackedByteArray()
var restore := false
func verify(ok: bool, what: String):
 if ok: passed += 1
 else:
  failed += 1
  push_error(what)
func frames(n := 3):
 for i in range(n): await process_frame
func key(code: Key):
 var e := InputEventKey.new()
 e.keycode = code
 e.physical_keycode = code
 e.pressed = true
 Input.parse_input_event(e)
 await frames(2)
 e = InputEventKey.new()
 e.keycode = code
 e.physical_keycode = code
 e.pressed = false
 Input.parse_input_event(e)
 await frames(3)
func _initialize(): call_deferred("run")
func _finalize():
 if not restore: return
 if had_settings:
  var f := FileAccess.open("user://settings.cfg",FileAccess.WRITE)
  f.store_buffer(settings_backup)
 else: DirAccess.remove_absolute(ProjectSettings.globalize_path("user://settings.cfg"))
func run():
 var audio = root.get_node("GameAudio")
 if "--probe-audio" in OS.get_cmdline_user_args():
  verify(is_equal_approx(audio.GetMusicVolume(),.35) and is_equal_approx(audio.GetEffectsVolume(),.65),"New process loads both volumes")
  print("AUDIO RESTART PROBE ","PASSED" if failed == 0 else "FAILED")
  await preload("res://Tests/AudioTestCleanup.gd").drain(self)
  quit(1 if failed else 0)
  return
 had_settings = FileAccess.file_exists("user://settings.cfg")
 if had_settings: settings_backup = FileAccess.get_file_as_bytes("user://settings.cfg")
 restore = true
 audio.SetMusicVolume(1)
 audio.SetEffectsVolume(1)
 change_scene_to_file("res://Scenes/UI/MainMenu.tscn")
 await frames()
 var settings_button = current_scene.get_node("CenterContainer/VBoxContainer/SettingsButton")
 settings_button.grab_focus()
 await key(KEY_ENTER)
 var settings = get_first_node_in_group("settings_menu")
 verify(settings != null and not current_scene.get_node("CenterContainer").visible,"Settings opens as modal")
 var box = settings.get_node("Center/Options")
 verify(root.gui_get_focus_owner() == box.get_node("Music"),"Music receives keyboard focus")
 await key(KEY_LEFT)
 verify(is_equal_approx(audio.GetMusicVolume(),.95) and is_equal_approx(audio.GetEffectsVolume(),1),"Left changes only music")
 await key(KEY_DOWN)
 await key(KEY_LEFT)
 verify(root.gui_get_focus_owner() == box.get_node("Effects") and is_equal_approx(audio.GetEffectsVolume(),.95),"Keyboard selects and adjusts effects")
 box.get_node("Effects").value = 0
 verify(AudioServer.is_bus_mute(AudioServer.get_bus_index("Effects")),"Zero effects truly mutes its bus")
 box.get_node("Music").value = 0
 verify(AudioServer.is_bus_mute(AudioServer.get_bus_index("Music")),"Zero music truly mutes its bus")
 await key(KEY_ESCAPE)
 verify(get_first_node_in_group("settings_menu") == null and root.gui_get_focus_owner() == settings_button,"Escape restores menu focus")
 audio.SetMusicVolume(.35)
 audio.SetEffectsVolume(.65)
 current_scene.get_node("CenterContainer/VBoxContainer/LanguageButton").emit_signal("pressed")
 var cfg := ConfigFile.new()
 cfg.load("user://settings.cfg")
 verify(is_equal_approx(cfg.get_value("audio","music",-1),.35) and cfg.has_section_key("interface","language"),"Audio and language preserve each other's settings")
 var output: Array = []
 var exit_code := OS.execute(OS.get_executable_path(),PackedStringArray(["--headless","--path",ProjectSettings.globalize_path("res://"),"--script","res://Tests/LocalAdjustmentsSmoke.gd","--","--probe-audio"]),output,true)
 verify(exit_code == 0 and str(output).contains("AUDIO RESTART PROBE PASSED"),"Both volumes survive full restart")
 current_scene.get_node("CenterContainer/VBoxContainer/PlayButton").emit_signal("pressed")
 await frames()
 await key(KEY_ESCAPE)
 var pause_menu = root.get_node("PauseMenu")
 pause_menu.get_node("CenterContainer/VBoxContainer/SettingsButton").grab_focus()
 await key(KEY_ENTER)
 settings = get_first_node_in_group("settings_menu")
 var run_time: float = root.get_node("DungeonManager").GetRunSeconds()
 await key(KEY_RIGHT)
 await frames(30)
 verify(paused and is_equal_approx(run_time,root.get_node("DungeonManager").GetRunSeconds()),"Settings preserves pause and run clock")
 await key(KEY_ESCAPE)
 verify(paused and is_instance_valid(pause_menu) and get_first_node_in_group("settings_menu") == null,"Settings Escape does not resume the run")
 await key(KEY_ESCAPE)
 verify(not paused,"Next Escape resumes normally")
 # Changing rooms or music fades must not overwrite the chosen bus gain.
 audio.SetMusicVolume(0)
 await frames(60)
 verify(AudioServer.is_bus_mute(AudioServer.get_bus_index("Music")),"Music fade cannot override mute")
 current_scene.queue_free()
 await frames()
 var arena := Node2D.new()
 root.add_child(arena)
 current_scene = arena
 var target := Node2D.new()
 target.add_to_group("player")
 target.position = Vector2(512,288)
 arena.add_child(target)
 # The eye keeps a frontal radial cast and all four shots expire at 240 pixels.
 var eye = load("res://Scenes/Enemies/EnemyFourWay.tscn").instantiate()
 eye.MoveSpeed = 0
 eye.position = Vector2(400,288)
 arena.add_child(eye)
 await frames(94)
 var art = eye.get_node("Visual/Sprite")
 verify(art.animation == &"attack_radial" and not art.flip_h,"Eye uses centered radial cast instead of side beam")
 var shots = get_nodes_in_group("projectiles")
 verify(shots.size() == 4,"Radial cast still produces all four shots")
 var origins := {}
 var endpoints := []
 for shot in shots:
  verify(shot.MaxTravelDistance == 240,"Eye projectile has limited range")
  origins[shot.get_instance_id()] = shot.position - Vector2.RIGHT.rotated(shot.rotation)*shot.GetDistanceTravelled()
  var origin: Vector2 = origins[shot.get_instance_id()]
  shot.tree_exiting.connect(func(): endpoints.append(shot.position.distance_to(origin)))
 eye.set_physics_process(false)
 await frames(90)
 verify(endpoints.size()==4,"All four eye shots expire")
 for distance in endpoints: verify(absf(distance-240)<.1,"Eye range is distance-based")
 eye.queue_free()
 await frames()
 # Boss damage changes color without replacing a walking/attacking pose.
 for name in ["Boss","BossMelee","BossRanged","BossFourWay","CastleBoss"]:
  var enemy = load("res://Scenes/Enemies/"+name+".tscn").instantiate()
  arena.add_child(enemy)
  enemy.set_physics_process(false)
  var visual = enemy.get_node("Visual")
  visual.SetMotion(Vector2.RIGHT*100,Vector2.RIGHT)
  await frames(2)
  enemy.TakeDamage(1)
  await frames(2)
  var sprite = visual.get_node("Sprite")
  verify(sprite.animation == &"walk_right" and sprite.self_modulate.g < .3,name+" keeps walking and flashes red")
  await frames(13)
  verify(sprite.self_modulate == Color.WHITE,name+" flash clears quickly")
  enemy.queue_free()
  await frames()
 # Follow two complete blink sequences and the normal-fire interval between them.
 var boss = load("res://Scenes/Enemies/BossRanged.tscn").instantiate()
 boss.BlinkSeed = 42
 boss.MoveSpeed = 0
 boss.position = Vector2(400,200)
 arena.add_child(boss)
 var last_teleports := 0
 var previous_position: Vector2 = boss.position
 var blink_shots := 0
 var normal_shots := 0
 var waiting := false
 var previous_stage := "Normal"
 var aim_started := 0
 var previous_completed := 0
 var last_completion := -1
 for frame in range(2400):
  await frames(1)
  var stage: String = boss.GetBlinkStage()
  if boss.GetBlinkTeleports() != last_teleports:
   verify(boss.position.distance_to(target.position)>=159.9,"Blink stays away from player")
   verify(boss.position.distance_to(previous_position)>=109.9,"Blink changes position substantially")
   verify(Rect2(328,104,368,368).has_point(boss.position),"Blink stays inside arena")
   previous_position = boss.position
   last_teleports = boss.GetBlinkTeleports()
   aim_started = frame
  if stage == "Vanish" and previous_stage == "Normal" and last_completion >= 0:
   verify(frame-last_completion >= 600,"Special waits at least ten seconds after previous sequence")
  shots = get_nodes_in_group("projectiles")
  for shot in shots:
   if stage == "Recovery":
    blink_shots += 1
    verify(frame-aim_started >= 31,"Arrow waits for reappearance warning")
    verify(shot.IsArrow,"Blink launches an arrow")
   elif waiting: normal_shots += 1
   shot.queue_free()
  if boss.GetBlinkSequences() > previous_completed:
   verify(boss.GetBlinkShots()==3,"Each blink sequence fires exactly three arrows")
   verify(boss.GetBlinkCooldown() >= 10 and boss.GetBlinkCooldown() <= 13,"Balanced post-sequence cooldown")
   previous_completed = boss.GetBlinkSequences()
   last_completion = frame
   waiting = true
   if previous_completed == 2: break
  previous_stage = stage
 verify(previous_completed == 2 and blink_shots == 6 and last_teleports == 6,"Two sequences each teleport and shoot three times")
 verify(normal_shots > 0,"Boss resumes its normal shots between sequences")
 # Pause and defeat must freeze/cancel the special.
 for i in range(1000):
  await frames(1)
  if boss.GetBlinkStage()=="Aim": break
 var position_before: Vector2 = boss.position
 var teleports_before: int = boss.GetBlinkTeleports()
 paused = true
 await frames(90)
 verify(boss.position==position_before and boss.GetBlinkTeleports()==teleports_before and boss.GetBlinkStage()=="Aim","Pause freezes teleport sequence")
 paused = false
 boss.TakeDamage(1000)
 await frames(40)
 verify(not is_instance_valid(boss),"Defeat cancels and cleans up teleport boss")
 print("LOCAL ADJUSTMENT CHECKS: ",passed," passed; ",failed," failed")
 await preload("res://Tests/AudioTestCleanup.gd").drain(self)
 quit(1 if failed else 0)
