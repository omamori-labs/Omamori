using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Build.Framework;
using Omamori.Core.Loading;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Omamori.ModBuildConfig
{
    public sealed class ValidateManifest : MSBuildTask
    {
        /// <summary>path to mod.json</summary>
        [Required]
        public string ManifestPath { get; set; } = string.Empty;

        /// <summary>the assembly this project builds, mod.json's entry must name it</summary>
        [Required]
        public string ExpectedEntry { get; set; } = string.Empty;

        /// <summary>OmamoriHarmony, a mod that can patch has to say so in its capabilities</summary>
        public bool UsesHarmony { get; set; }

        [Output]
        public string ModId { get; private set; } = string.Empty;

        [Output]
        public string ModVersion { get; private set; } = string.Empty;

        public override bool Execute()
        {
            if (!File.Exists(ManifestPath))
            {
                error("OMA001", $"no mod.json next to the project, every mod needs one (expected {ManifestPath})");
                return false;
            }

            string json;

            try
            {
                json = File.ReadAllText(ManifestPath);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                error("OMA006", $"couldn't read mod.json: {e.Message}");
                return false;
            }

            JsonDocument document;

            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException e)
            {
                error("OMA002", $"mod.json isn't valid json: {e.Message}", (int)(e.LineNumber ?? 0) + 1);
                return false;
            }

            using (document)
            {
                foreach (string problem in ManifestSchema.Validate(document.RootElement))
                    error("OMA003", problem);

                if (Log.HasLoggedErrors)
                    return false;

                var root = document.RootElement;
                ModId = root.GetProperty("id").GetString()!;
                ModVersion = root.GetProperty("version").GetString()!.Trim();

                string entry = root.GetProperty("entry").GetString()!;

                if (!string.Equals(entry, ExpectedEntry, StringComparison.Ordinal))
                    error("OMA004", $"entry is \"{entry}\" but this project builds {ExpectedEntry}");

                if (ManifestRules.ApiProblem(root.GetProperty("apiVersion").GetString()) is string apiProblem)
                    error("OMA005", $"apiVersion: {apiProblem}");

                if (root.TryGetProperty("dependencies", out var dependencies)
                    && ManifestRules.DependsOnItself(ModId, dependencies.EnumerateArray().Select(d => d.GetProperty("id").GetString())))
                    error("OMA007", "dependencies lists this mod's own id, a mod can't depend on itself");

                if (UsesHarmony && !ManifestRules.DeclaresPatches(root.GetProperty("capabilities").EnumerateArray().Select(c => c.GetString())))
                    error("OMA008", "OmamoriHarmony is on, add \"Patches\" to capabilities so players know this mod changes osu!'s code");
            }

            return !Log.HasLoggedErrors;
        }

        private void error(string code, string message, int line = 0) =>
            Log.LogError(null, code, null, ManifestPath, line, 0, 0, 0, message);
    }
}
