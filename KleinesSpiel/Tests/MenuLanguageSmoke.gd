extends SceneTree
# Uses actual keyboard events, including while the game is paused. A second
# Godot process verifies that changing language survives a complete restart.
var passed := 0
var failed := 0
var original_settings := PackedByteArray()
var had_settings := false
var restore_settings := false
const SETTINGS := "user://settings.cfg"
const BOX := "CenterContainer/VBoxContainer/"
const ITEM_PATHS := [
 "res://Resources/Items/Item01_ExtraHeart.tres",
 "res://Resources/Items/Item02_MoveSpeed.tres",
 "res://Resources/Items/Item03_FireRate.tres",
 "res://Resources/Items/Item04_Damage.tres",
 "res://Resources/Items/Item05_Piercing.tres",
 "res://Resources/Items/Item06_ArcaneLens.tres"
]

func verify(condition: bool, description: String) -> void:
 if condition:
  passed += 1
 else:
  failed += 1
  push_error(description)

func frames(count: int = 3) -> void:
 for i in range(count):
  await process_frame

func key(code: Key) -> void:
 var event := InputEventKey.new()
 event.keycode = code
 event.physical_keycode = code
 event.pressed = true
 Input.parse_input_event(event)
 await frames(2)
 event = InputEventKey.new()
 event.keycode = code
 event.physical_keycode = code
 event.pressed = false
 Input.parse_input_event(event)
 await frames(3)

func button(menu: Node, name_: String) -> Button:
 return menu.get_node(BOX + name_) as Button

func menu_cycle(menu: Node, names: Array) -> void:
 var first := button(menu, names[0])
 verify(root.gui_get_focus_owner() == first, menu.name + " has an initial keyboard selection")
 for code in [KEY_DOWN, KEY_RIGHT]:
  first.grab_focus()
  for i in range(1, names.size() + 1):
   await key(code)
   verify(root.gui_get_focus_owner() == button(menu, names[i % names.size()]), menu.name + " cycles forward with " + str(code))
 for code in [KEY_UP, KEY_LEFT]:
  first.grab_focus()
  for i in range(1, names.size() + 1):
   await key(code)
   verify(root.gui_get_focus_owner() == button(menu, names[posmod(-i, names.size())]), menu.name + " cycles backward with " + str(code))
 first.grab_focus()

func _initialize() -> void:
 call_deferred("run")

func _finalize() -> void:
 if not restore_settings:
  return
 if had_settings:
  var file := FileAccess.open(SETTINGS, FileAccess.WRITE)
  file.store_buffer(original_settings)
 else:
  DirAccess.remove_absolute(ProjectSettings.globalize_path(SETTINGS))

func restart_probe(language: String) -> void:
 var settings := ConfigFile.new()
 verify(settings.load(SETTINGS) == OK and settings.get_value("interface", "language", "") == language, "Language preference is saved to disk")
 var output: Array = []
 var code := OS.execute(OS.get_executable_path(), PackedStringArray([
  "--headless", "--path", ProjectSettings.globalize_path("res://"),
  "--script", "res://Tests/MenuLanguageSmoke.gd", "--", "--probe-language=" + language
 ]), output, true)
 verify(code == 0 and str(output).contains("LANGUAGE RESTART PROBE PASSED"), "Fresh Godot process loads saved " + language + " preference: " + str(output))

func item_text(language: String) -> void:
 var expected_names := ["Heart Medallion", "Winged Boot", "Spell Hourglass", "Ruby Blade", "Piercing Tip", "Arcane Lens"] if language == "en" else ["Herzmedaillon", "Geflügelter Stiefel", "Sanduhr des Zaubers", "Rubinklinge", "Durchdringende Spitze", "Arkane Linse"]
 for i in range(ITEM_PATHS.size()):
  var data = load(ITEM_PATHS[i])
  verify(data.GetDisplayName() == expected_names[i], "Passive " + str(i) + " has its " + language + " name")
  var expected: String = data.EffectDescription if language == "en" else data.GermanEffectDescription
  verify(not expected.is_empty() and data.GetEffectDescription() == expected, "Passive " + str(i) + " has its " + language + " effect description")

func run() -> void:
 for argument in OS.get_cmdline_user_args():
  if argument.begins_with("--probe-language="):
   change_scene_to_file("res://Scenes/UI/MainMenu.tscn")
   await frames()
   var expected := argument.get_slice("=", 1)
   if current_scene.GetLanguageCode() == expected:
    print("LANGUAGE RESTART PROBE PASSED: " + expected)
    quit(0)
   else:
    quit(1)
   return
 had_settings = FileAccess.file_exists(SETTINGS)
 if had_settings:
  original_settings = FileAccess.get_file_as_bytes(SETTINGS)
  DirAccess.remove_absolute(ProjectSettings.globalize_path(SETTINGS))
 restore_settings = true
 change_scene_to_file("res://Scenes/UI/MainMenu.tscn")
 await frames()
 verify(current_scene.GetLanguageCode() == "en", "First launch defaults to English")
 verify(button(current_scene, "PlayButton").text == "Play", "Main menu starts in English")
 await menu_cycle(current_scene, ["PlayButton", "LanguageButton"])
 await key(KEY_DOWN)
 await key(KEY_ENTER)
 verify(current_scene.GetLanguageCode() == "de", "Enter activates the language button")
 verify(button(current_scene, "PlayButton").text == "Spielen" and button(current_scene, "LanguageButton").text == "Sprache: Deutsch", "Main menu changes immediately to German")
 verify(root.gui_get_focus_owner() == button(current_scene, "LanguageButton"), "Language change keeps the selected button")
 restart_probe("de")
 await key(KEY_SPACE)
 verify(current_scene.GetLanguageCode() == "en", "Space switches language back to English")
 restart_probe("en")

 for language in ["en", "de"]:
  if current_scene.GetLanguageCode() != language:
   button(current_scene, "LanguageButton").grab_focus()
   await key(KEY_ENTER)
  item_text(language)
  button(current_scene, "PlayButton").grab_focus()
  await key(KEY_ENTER)
  verify(current_scene.name == "Main", "Keyboard starts a run in " + language)
  var player = get_first_node_in_group("player")
  var hud = get_first_node_in_group("hud")
  verify(hud.get_node("HealthLabel").text.begins_with("Health:" if language == "en" else "Leben:"), "HUD health uses " + language)
  verify(hud.get_node("MultiplierLabel").text.begins_with("Multiplier:" if language == "en" else "Multiplikator:"), "HUD multiplier uses " + language)
  verify(hud.get_node("CoinsLabel").text.begins_with("Coins:" if language == "en" else "Münzen:"), "Coin balance uses " + language)
  var item = load("res://Scenes/Items/ItemPickup.tscn").instantiate()
  var item_data = load(ITEM_PATHS[3])
  item.Data = item_data
  item.position = player.position
  current_scene.add_child(item)
  await frames(8)
  verify(hud.get_node("ItemPopupLabel").text == item_data.GetDisplayName() + "\n" + item_data.GetEffectDescription(), "Collecting a passive shows both translated name and effect")
  await key(KEY_ESCAPE)
  var pause_menu = root.get_node_or_null("PauseMenu")
  verify(paused and pause_menu != null, "Escape opens the pause menu")
  if pause_menu == null:
   quit(1)
   return
  verify(pause_menu.get_node(BOX + "TitleLabel").text == ("Paused" if language == "en" else "Pausiert"), "Pause title uses " + language)
  var old_position: Vector2 = player.position
  await menu_cycle(pause_menu, ["ContinueButton", "RetryButton", "MenuButton"])
  verify(player.position == old_position, "Navigating paused menus does not move gameplay")
  await key(KEY_SPACE)
  verify(not paused and not is_instance_valid(pause_menu), "Space resumes the paused game")
  verify(player.get_node("Visual").GetAnimationState() != "dash", "Space confirmation does not trigger a gameplay dash")
  await key(KEY_ESCAPE)
  await key(KEY_DOWN)
  var old_game = current_scene
  await key(KEY_ENTER)
  verify(not paused and not is_instance_valid(old_game) and current_scene.name == "Main", "Pause Retry starts a fresh run using keyboard")
  await key(KEY_ESCAPE)
  await key(KEY_DOWN)
  await key(KEY_DOWN)
  await key(KEY_ENTER)
  verify(current_scene.name == "MainMenu" and not paused, "Pause Main menu returns using keyboard")

  for ending in ["GameOverScreen", "VictoryScreen"]:
   for target in ["RetryButton", "MenuButton"]:
    if current_scene.name == "MainMenu":
     button(current_scene, "PlayButton").grab_focus()
     await key(KEY_ENTER)
    var overlay = load("res://Scenes/UI/" + ending + ".tscn").instantiate()
    root.add_child(overlay)
    paused = true
    await frames()
    await menu_cycle(overlay, ["RetryButton", "MenuButton"])
    var title: String = overlay.get_node(BOX + "TitleLabel").text
    verify(title == (("Game over" if language == "en" else "Spiel vorbei") if ending == "GameOverScreen" else ("Victory!" if language == "en" else "Sieg!")), ending + " title uses " + language)
    if target == "MenuButton":
     await key(KEY_RIGHT)
    old_game = current_scene
    await key(KEY_ENTER)
    verify(not paused and not is_instance_valid(overlay) and not is_instance_valid(old_game), ending + " keyboard selection closes overlay and old run")
    verify(current_scene.name == ("Main" if target == "RetryButton" else "MainMenu"), ending + " activates " + target)
 print("MENU / LANGUAGE CHECKS: %d passed; %d failed" % [passed, failed])
 quit(0 if failed == 0 else 1)
