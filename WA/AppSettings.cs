namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    [Serializable]
    public class AppSettingsData
    {
        public List<string> PluginDirectories { get; set; } = new List<string>() { @"plugins\spi\" };

        public bool EnableBuiltInDecoders { get; set; } = true;

        public bool EnableLogging { get; set; } = true;

    }

    public class AppSettings
    {
        private const string _name = "WA.Settings.json";

        private static AppSettingsData _backup;

        public AppSettingsData Data { get; private set; }

        public AppSettings()
        {
        }

        public static AppSettings Load(bool reset = false)
        {
            string path = GetSettingsPath();
            AppSettings settings = new AppSettings();
            if (!reset && File.Exists(path))
            {
                settings.Data = JsonUtility.LoadFromFile<AppSettingsData>(path);
                _backup = JsonUtility.Clone(settings.Data); // fixme 効率が悪い
            }
            else
            {
                settings.Data = new AppSettingsData();
                _backup = new AppSettingsData();
            }

            return settings;
        }

        public static void Revert(AppSettings settings)
        {
            // instanceは維持してデータだけ差し替え
            settings.Data = JsonUtility.Clone(_backup); // fixme 効率が悪い
        }

        public static async Task<AppSettings> LoadAsync(bool reset = false)
        {
            string path = GetSettingsPath();
            AppSettings settings = new AppSettings();
            if (!reset && File.Exists(path))
            {
                settings.Data = await JsonUtility.LoadFromFileAsync<AppSettingsData>(path);
                _backup = await JsonUtility.CloneAsync(settings.Data); // fixme 効率が悪い
            }
            else
            {
                settings.Data = new AppSettingsData();
                _backup = new AppSettingsData();
            }

            return settings;
        }

        public void Save()
        {
            // todo if only changed
            string path = GetSettingsPath();
            JsonUtility.SaveToFile(path, this.Data);
            _backup = JsonUtility.Clone(this.Data); // fixme 効率が悪い
        }

        public async Task SaveAsync()
        {
            // todo if only changed
            string path = GetSettingsPath();
            await JsonUtility.SaveToFileAsync(path, this.Data)
                .ContinueWith(_ => _backup = JsonUtility.Clone(this.Data)); // fixme 効率が悪い
        }

        private static string GetSettingsPath()
        {
            var directory = DirectoryUtility.GetBaseDirectory();
            var path = Path.Combine(directory, _name);
            return path;
        }
    }
}
