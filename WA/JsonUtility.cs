// (c) longod, MIT License
namespace WA
{
    using System.IO;
    using System.Threading.Tasks;
    using Utf8Json;

    internal static class JsonUtility
    {
        internal static T LoadFromFile<T>(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return JsonSerializer.Deserialize<T>(stream);
            }
        }

        internal static async Task<T> LoadFromFileAsync<T>(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return await JsonSerializer.DeserializeAsync<T>(stream);
            }
        }

        internal static void SaveToFile<T>(string path, T value)
        {
            using (var stream = File.OpenWrite(path))
            {
                JsonSerializer.Serialize(stream, value);
            }
        }

        internal static async Task SaveToFileAsync<T>(string path, T value)
        {
            using (var stream = File.OpenWrite(path))
            {
                await JsonSerializer.SerializeAsync(stream, value);
            }
        }

        internal static T Clone<T>(T value, int capasity = 2048)
        {
            using (var stream = new MemoryStream(capasity))
            {
                JsonSerializer.Serialize(stream, value);
                return JsonSerializer.Deserialize<T>(stream);
            }
        }

        internal static async Task<T> CloneSync<T>(T value, int capasity = 2048)
        {
            using (var stream = new MemoryStream(capasity))
            {
                return await JsonSerializer.SerializeAsync(stream, value).ContinueWith(_ => JsonSerializer.Deserialize<T>(stream));
            }
        }
    }
}
