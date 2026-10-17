using Godot;
using System.Globalization;

namespace KleinesSpiel.Scripts.Common
{
    // Keep the preference outside the project so importing an updated ZIP preserves it.
    public static class Localization
    {
        public const string SettingsPath = "user://settings.cfg";
        private static string _language;

        public static string Language
        {
            get
            {
                if (_language == null)
                {
                    var settings = new ConfigFile();
                    _language = settings.Load(SettingsPath) == Error.Ok
                        && settings.GetValue("interface", "language", "en").AsString() == "de" ? "de" : "en";
                }
                return _language;
            }
        }

        public static string Text(string english, string german) => Language == "de" ? german : english;
        public static string Format(string english, string german, params object[] values) =>
            string.Format(CultureInfo.InvariantCulture, Text(english, german), values);

        public static void ToggleLanguage()
        {
            _language = Language == "en" ? "de" : "en";
            var settings = new ConfigFile();
            settings.Load(SettingsPath);
            settings.SetValue("interface", "language", _language);
            var result = settings.Save(SettingsPath);
            if (result != Error.Ok) GD.PushWarning($"Could not save language preference: {result}");
        }
    }
}
