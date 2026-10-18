using Godot;
using KleinesSpiel.Scripts.Common;
namespace KleinesSpiel.Scripts.UI
{
    public partial class RunClockLabel : Label
    {
        public override void _Ready() { ProcessMode = ProcessModeEnum.Always; MouseFilter = MouseFilterEnum.Ignore; }
        public override void _Process(double delta)
        {
            Visible = Input.IsPhysicalKeyPressed(Key.Tab);
            var elapsed = System.TimeSpan.FromSeconds(Rooms.DungeonManager.Instance?.GetRunSeconds() ?? 0);
            Text = Localization.Text("Run time: ", "Spielzeit: ") + $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        }
    }
}
