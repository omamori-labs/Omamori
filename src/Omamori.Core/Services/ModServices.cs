using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using Omamori.Core.Safety;

namespace Omamori.Core.Services
{
    internal sealed class ModServices : IModServices
    {
        private static readonly List<(string ModId, Type Type, object Service)> provided = [];

        private readonly ModManifest manifest;
        private readonly IModLogger log;
        private readonly HashSet<Type> warned = [];

        public ModServices(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            this.log = log;
        }

        public void Provide<T>(T service)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(service);

            lock (provided)
            {
                if (provided.Any(p => p.ModId == manifest.Id && p.Type == typeof(T)))
                    throw new InvalidOperationException($"{typeof(T).FullName} is already provided by this mod");

                provided.Add((manifest.Id, typeof(T), service));
            }
        }

        public T? Get<T>()
            where T : class
        {
            List<(string ModId, Type Type, object Service)> candidates;

            lock (provided)
                candidates = [.. provided.Where(p => !ModFaults.IsFaulted(p.ModId))];

            if (candidates.FirstOrDefault(p => p.Type == typeof(T)) is { Service: T service })
                return service;

            // same name, different type, the mod bundled its own copy of the provider's dll
            if (candidates.FirstOrDefault(p => p.Type.FullName == typeof(T).FullName) is { ModId: not null } copy && firstWarning(typeof(T)))
            {
                log.Error($"{copy.ModId} provides {typeof(T).FullName} but this mod has its own copy of that type. "
                          + $"Declare {copy.ModId} as a dependency and reference its dll with OmamoriModReference instead of bundling it.");
            }

            return null;
        }

        private bool firstWarning(Type type)
        {
            lock (warned)
                return warned.Add(type);
        }

        internal static void Clear()
        {
            lock (provided)
                provided.Clear();
        }
    }
}
