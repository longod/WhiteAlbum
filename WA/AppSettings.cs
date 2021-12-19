namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    [Serializable]
    public class AppSettings
    {
        public List<string> PluginDirectories { get; set; } = new List<string>() { @"plugins\spi\" };

        public bool EnableBuiltInDecoders { get; set; } = true;

        public bool EnableLogging { get; set; } = true;

        private const string _name = "WA.Settings.json";

        private static AppSettings _backup;

        public AppSettings()
        {
        }

        public static AppSettings Load(bool reset = false)
        {
            string path = GetSettingsPath();
            AppSettings settings = null;
            if (!reset && File.Exists(path))
            {
                settings = JsonUtility.LoadFromFile<AppSettings>(path);
                _backup = JsonUtility.Clone(settings); // fixme 効率が悪い
            }
            else
            {
                settings = new AppSettings();
                _backup = new AppSettings();
            }

            return settings;
        }

        public static async Task<AppSettings> LoadAsync(bool reset = false)
        {
            string path = GetSettingsPath();
            if (!reset && File.Exists(path))
            {
                return await JsonUtility.LoadFromFileAsync<AppSettings>(path);
            }

            return new AppSettings();
        }

        public void Save()
        {
            // todo if only changed
            string path = GetSettingsPath();
            JsonUtility.SaveToFile(path, this);
            _backup = JsonUtility.Clone(this); // fixme 効率が悪い
        }

        public async Task SaveAsync()
        {
            // todo if only changed
            string path = GetSettingsPath();
            await JsonUtility.SaveToFileAsync(path, this)
                .ContinueWith(_ => _backup = JsonUtility.Clone(this)); // fixme 効率が悪い
        }

        private static string GetSettingsPath()
        {
            // fixme いまどきは AppData に読み書きするのが正しい, publish先もそこが妥当
            var directory = AppContext.BaseDirectory;
            var path = Path.Combine(directory, _name);
            return path;
        }
    }
}
