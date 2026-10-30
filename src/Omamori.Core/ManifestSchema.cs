using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Json.Schema;

namespace Omamori.Core
{
    /// <summary>
    ///     validates mod.json against schemas/mod.schema.json
    /// </summary>
    internal static class ManifestSchema
    {
        private const string resource_name = "Omamori.Core.mod.schema.json";

        private const int max_reported = 3;

        private static readonly Lazy<JsonSchema> schema = new Lazy<JsonSchema>(load);

        /// <returns>empty when valid otherwise one readable line per problem</returns>
        public static List<string> Validate(JsonElement manifest)
        {
            var result = schema.Value.Evaluate(manifest, new EvaluationOptions { OutputFormat = OutputFormat.List });

            if (result.IsValid)
                return [];

            return
            [
                .. (result.Details ?? [])
                   .Where(d => d.Errors != null && d.InstanceLocation.ToString().Length > 0)
                   .SelectMany(d => d.Errors!.Select(e => describe(d.InstanceLocation.ToString(), e.Key, e.Value)))
                   .Where(p => p != null)
                   .Distinct()
                   .Cast<string>()
            ];
        }

        public static string Summarise(List<string> problems) =>
            string.Join("; ", problems.Take(max_reported)) + (problems.Count > max_reported ? $" (+{problems.Count - max_reported} more)" : string.Empty);

        private static string? describe(string location, string keyword, string message) => keyword switch
        {
            "properties" or "items" or "additionalProperties" => null,
            "" => $"{location} isn't a known property",
            "pattern" => $"{location} doesn't match the expected format",
            _ => $"{location}: {message}",
        };

        private static JsonSchema load()
        {
            using var stream = typeof(ManifestSchema).Assembly.GetManifestResourceStream(resource_name)
                               ?? throw new InvalidOperationException($"embedded {resource_name} is missing, core was built wrong");
            using var reader = new StreamReader(stream);

            return JsonSchema.FromText(reader.ReadToEnd());
        }
    }
}
