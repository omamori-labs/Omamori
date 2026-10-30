using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Omamori.Core
{
    /// <summary>
    ///     omamori's own state files, a missing or unreadable file starts fresh and a failed save only logs
    /// </summary>
    internal static class JsonFile
    {
        private static readonly JsonSerializerOptions json_options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static T Read<T>(string path, string fallback)
            where T : class, new()
        {
            try
            {
                if (File.Exists(path))
                    return JsonSerializer.Deserialize<T>(File.ReadAllText(path), json_options) ?? new T();
            }
            catch (Exception e)
            {
                LoaderLog.Error($"Couldn't read {path}, {fallback}.", e);
            }

            return new T();
        }

        public static void Write<T>(string path, T value)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonSerializer.Serialize(value, json_options));
                File.Move(temporary, path, overwrite: true);
            }
            catch (Exception e)
            {
                LoaderLog.Error($"Couldn't save {path}.", e);
            }
        }
    }
}
