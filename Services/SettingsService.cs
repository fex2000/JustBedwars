
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace JustBedwars.Services
{
    public class SettingsService
    {
        private readonly string _filePath;
        private Dictionary<string, object> _settings;

        public event EventHandler<string> SettingChanged;

        public SettingsService()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsFolder = Path.Combine(appDataFolder, "JustBedwars");
            Directory.CreateDirectory(settingsFolder);
            _filePath = Path.Combine(settingsFolder, "settings.json");
            Load();
        }

        private void Load()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            }
            else
            {
                _settings = new Dictionary<string, object>();
            }
        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public object GetValue(string key)
        {
            return _settings.TryGetValue(key, out var value) ? value : null;
        }

        public void SetValue(string key, object value)
        {
            _settings[key] = value;
            Save();
            SettingChanged?.Invoke(this, key);
        }
    }
}
