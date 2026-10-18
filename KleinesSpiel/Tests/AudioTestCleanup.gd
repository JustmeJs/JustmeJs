extends RefCounted
# Fixed-FPS headless runs advance game time faster than the audio mixer thread.
# Stop voices and allow the real mixer to retire playback handles before shutdown.
static func drain(tree: SceneTree) -> void:
 tree.paused = true
 tree.root.get_node("GameAudio").OnRunEnded()
 var deadline := Time.get_ticks_msec() + 100
 while Time.get_ticks_msec() < deadline:
  await tree.process_frame
