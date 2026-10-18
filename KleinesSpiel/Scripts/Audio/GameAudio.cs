using Godot;
using System.Collections.Generic;
using KleinesSpiel.Scripts.Rooms;

namespace KleinesSpiel.Scripts.Audio
{
    public partial class GameAudio : Node
    {
        public static GameAudio Instance { get; private set; }
        private AudioStreamPlayer _music;
        private readonly List<AudioStreamPlayer> _voices = new();
        private readonly Dictionary<string, AudioStream> _streams = new();
        private readonly Dictionary<string, ulong> _lastEffect = new();
        private Tween _fade;
        private bool _ended = true;
        private string _track = "";
        public string GetCurrentTrack() => _track;
        public bool IsMusicPaused() => _music.StreamPaused;
        public override void _Ready()
        {
            Instance = this;
            ProcessMode = ProcessModeEnum.Always;
            _music = new AudioStreamPlayer { Name = "Music", VolumeDb = -15 };
            AddChild(_music);
            for (int i = 0; i < 8; i++)
            {
                var voice = new AudioStreamPlayer { Name = $"Effect{i}", VolumeDb = -16 };
                AddChild(voice); _voices.Add(voice);
            }
        }
        public override void _Process(double delta)
        {
            _music.StreamPaused = GetTree().Paused && !_ended;
            foreach (var voice in _voices) voice.StreamPaused = GetTree().Paused && !_ended;
        }
        private static readonly Dictionary<string, string> Paths = new()
        {
            ["arrow"] = "res://Assets/Audio/arrow.wav",
            ["cast"] = "res://Assets/Audio/cast.wav",
            ["castle_archer"] = "res://Assets/Audio/castle_archer.ogg",
            ["castle_champion"] = "res://Assets/Audio/castle_champion.ogg",
            ["castle_rooms"] = "res://Assets/Audio/castle_rooms.ogg",
            ["castle_warden"] = "res://Assets/Audio/castle_warden.ogg",
            ["charge"] = "res://Assets/Audio/charge.wav",
            ["crypt_archer"] = "res://Assets/Audio/crypt_archer.ogg",
            ["crypt_melee"] = "res://Assets/Audio/crypt_melee.ogg",
            ["crypt_ritual"] = "res://Assets/Audio/crypt_ritual.ogg",
            ["crypt_rooms"] = "res://Assets/Audio/crypt_rooms.ogg",
            ["death"] = "res://Assets/Audio/death.wav",
            ["enemy_cast"] = "res://Assets/Audio/enemy_cast.wav",
            ["enemy_death"] = "res://Assets/Audio/enemy_death.wav",
            ["hurt"] = "res://Assets/Audio/hurt.wav",
            ["impact"] = "res://Assets/Audio/impact.wav",
            ["melee"] = "res://Assets/Audio/melee.wav",
            ["regent"] = "res://Assets/Audio/regent.ogg",
        };
        private AudioStream Load(string name, bool music)
        {
            if (_streams.TryGetValue(name, out var result)) return result;
            result = GD.Load<AudioStream>(Paths[name]);
            if (result is AudioStreamOggVorbis ogg) ogg.Loop = true;
            _streams[name] = result;
            return result;
        }
        public void EnterRoom(DungeonFloor floor, Room room)
        {
            _ended = false;
            string track = floor.IsCastle ? "castle_rooms" : "crypt_rooms";
            if (room.Definition.Kind == RoomMapType.Boss && !room.IsCleared())
            {
                string boss = System.IO.Path.GetFileNameWithoutExtension(floor.BossScene);
                track = floor.IsFinal ? "regent" : floor.IsCastle
                    ? boss == "BossRanged" ? "castle_archer" : boss == "BossFourWay" ? "castle_warden" : "castle_champion"
                    : boss == "BossMelee" ? "crypt_melee" : boss == "BossRanged" ? "crypt_archer" : "crypt_ritual";
            }
            if (track == _track && _music.Playing) return;
            _track = track;
            _fade?.Kill();
            _music.Stream = Load(track, true); _music.VolumeDb = -30; _music.StreamPaused = false; _music.Play();
            _fade = CreateTween(); _fade.TweenProperty(_music, "volume_db", -15f, .65);
        }
        public void OnRunEnded()
        {
            _ended = true; _track = ""; _fade?.Kill(); _music.Stop();
            foreach (var voice in _voices) voice.Stop();
        }
        public void PlayEffect(string name)
        {
            // Polyphony and per-effect cooldown keep volleys from becoming loud bursts.
            ulong now = Time.GetTicksMsec();
            if (_lastEffect.TryGetValue(name, out ulong last) && now - last < 65) return;
            _lastEffect[name] = now;
            var voice = _voices.Find(v => !v.Playing);
            if (voice == null) { if (name != "death") return; voice = _voices[0]; voice.Stop(); }
            voice.Stream = Load(name, false);
            voice.VolumeDb = name == "death" ? -18 : name == "impact" || name == "enemy_death" ? -22 : -16;
            voice.StreamPaused = false;
            voice.Play();
        }
        public override void _ExitTree()
        {
            _fade?.Kill();
            _music.StreamPaused = false; _music.Stop(); _music.Stream = null;
            foreach (var voice in _voices) { voice.StreamPaused = false; voice.Stop(); voice.Stream = null; }
            foreach (var stream in _streams.Values) stream.Dispose();
            _streams.Clear();
            Enemies.EnemyAppearance.ClearCache();
            if (Instance == this) Instance = null;
        }
    }
}
