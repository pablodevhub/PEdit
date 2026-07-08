using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PEdit.Services
{
    public class ConfigurationService : INotifyPropertyChanged
    {
        public static ConfigurationService Instance { get; } = new ConfigurationService();
        private readonly string _configFilePath;
        private Dictionary<string, string> _settings;

        public string FontFamily { get => GetValue("FONT_FAMILY", "Consolas"); set { SetValue("FONT_FAMILY", value); OnPropertyChanged(); } }
        public string FontSize { get => GetValue("FONT_SIZE", "14"); set { SetValue("FONT_SIZE", value); OnPropertyChanged(); } }
        public string BackgroundColor { get => GetValue("BG_COLOR", "#1E1E1E"); set { SetValue("BG_COLOR", value); OnPropertyChanged(); } }
        public string ForegroundColor { get => GetValue("FG_COLOR", "#D4D4D4"); set { SetValue("FG_COLOR", value); OnPropertyChanged(); } }

        public bool SelectWholeWordOnDoubleClick
        {
            get => GetValue("SELECT_WHOLE_WORD_DOUBLE_CLICK", "False") == "True";
            set { SetValue("SELECT_WHOLE_WORD_DOUBLE_CLICK", value ? "True" : "False"); OnPropertyChanged(); }
        }

        public bool SearchFromBeginning
        {
            get => GetValue("SEARCH_ORIGIN", "Beginning") == "Beginning";
            set { SetValue("SEARCH_ORIGIN", value ? "Beginning" : "Cursor"); OnPropertyChanged(); OnPropertyChanged(nameof(SearchFromCursor)); }
        }

        public bool SearchFromCursor
        {
            get => !SearchFromBeginning;
            set { SearchFromBeginning = !value; }
        }

        private ConfigurationService()
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            _settings = new Dictionary<string, string>();
            Load();
        }

        public void Load()
        {
            if (File.Exists(_configFilePath))
            {
                var lines = File.ReadAllLines(_configFilePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";")) continue;
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2) _settings[parts[0].Trim()] = parts[1].Trim();
                }
            }
            EnsureDefaultsAndSave();
        }

        public void Save()
        {
            var lines = _settings.Select(kvp => $"{kvp.Key}={kvp.Value}");
            File.WriteAllLines(_configFilePath, lines);
        }

        private string GetValue(string key, string defaultValue = "") => _settings.TryGetValue(key, out string value) ? value : defaultValue;
        private void SetValue(string key, string value) { _settings[key] = value; Save(); }

        private void EnsureDefaultsAndSave()
        {
            bool changed = false;
            if (!_settings.ContainsKey("FONT_FAMILY")) { _settings["FONT_FAMILY"] = "Consolas"; changed = true; }
            if (!_settings.ContainsKey("FONT_SIZE")) { _settings["FONT_SIZE"] = "14"; changed = true; }
            if (!_settings.ContainsKey("BG_COLOR")) { _settings["BG_COLOR"] = "#1E1E1E"; changed = true; }
            if (!_settings.ContainsKey("FG_COLOR")) { _settings["FG_COLOR"] = "#D4D4D4"; changed = true; }
            if (!_settings.ContainsKey("SELECT_WHOLE_WORD_DOUBLE_CLICK")) { _settings["SELECT_WHOLE_WORD_DOUBLE_CLICK"] = "False"; changed = true; }
            if (!_settings.ContainsKey("SEARCH_ORIGIN")) { _settings["SEARCH_ORIGIN"] = "Beginning"; changed = true; }

            if (changed) Save();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}