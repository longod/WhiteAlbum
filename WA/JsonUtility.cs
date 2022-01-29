namespace WA
{
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;

    internal static class JsonUtility
    {
        internal static T LoadFromFile<T>(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                byte[] json = new byte[stream.Length];
                stream.Read(json);
                return JsonSerializer.Deserialize<T>(json);
            }
        }

        internal static async Task<T> LoadFromFileAsync<T>(string path)
        {
            await using (var stream = File.OpenRead(path))
            {
                return await JsonSerializer.DeserializeAsync<T>(stream);
            }
        }

        internal static void SaveToFile<T>(string path, T value)
        {
            using (var stream = File.Create(path))
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    JsonSerializer.Serialize(writer, value);
                }
            }
        }

        internal static async Task SaveToFileAsync<T>(string path, T value)
        {
            await using (var stream = File.OpenWrite(path))
            {
                await JsonSerializer.SerializeAsync(stream, value);
            }
        }

        internal static T Clone<T>(T value)
        {
            var json = JsonSerializer.Serialize(value);
            return JsonSerializer.Deserialize<T>(json);
        }

        internal static async Task<T> CloneAsync<T>(T value, int capasity = 2048)
        {
            await using (var stream = new MemoryStream(capasity))
            {
                await JsonSerializer.SerializeAsync(stream, value);
                return await JsonSerializer.DeserializeAsync<T>(stream);
            }
        }
    }
}
