using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Omamori.Core.Safety;
using osu.Game.Skinning;

namespace Omamori.Core.ModUi
{
    /// <summary>
    ///     mod components in skin layouts, lazer saves each by its assembly qualified type name
    ///     a layout naming a type that can't be found fails to load as a whole, so elements whose mod isn't loaded are left out first
    /// </summary>
    internal static class SkinComponents
    {
        private static readonly List<(string ModId, Type Type)> registrations = [];

        private static bool resolving;

        // set once mods have loaded, update thread only
        private static SkinManager? skins;

        public static void Add(string modId, Type type)
        {
            lock (registrations)
            {
                registrations.Add((modId, type));

                if (!resolving)
                {
                    AssemblyLoadContext.Default.Resolving += (_, name) => Resolve(name);
                    resolving = true;
                }
            }
        }

        /// <summary>
        ///     mods load in their own contexts, so a layout naming a mod's assembly only finds it here, by name whatever its version
        /// </summary>
        public static Assembly? Resolve(AssemblyName name)
        {
            lock (registrations)
                return registrations.Select(r => r.Type.Assembly).FirstOrDefault(a => string.Equals(a.GetName().Name, name.Name, StringComparison.Ordinal));
        }

        /// <summary>the skin editor's toolbox, with the components of mods that are still running</summary>
        public static Type[] WithModComponents(Type[] available)
        {
            lock (registrations)
                return [.. available, .. registrations.Where(r => !ModFaults.IsFaulted(r.ModId)).Select(r => r.Type).Where(t => !available.Contains(t)).Distinct()];
        }

        /// <summary>
        ///     the layout without the elements whose mod isn't loaded or has faulted, the same string when nothing is left out
        ///     an element naming a type omamori doesn't know in an assembly that loads is left for lazer to deal with like it always would
        /// </summary>
        public static string Filter(string json, out int leftOut)
        {
            leftOut = 0;
            JToken root;

            try
            {
                root = JToken.Parse(json);
            }
            catch (JsonException)
            {
                return json;
            }

            leftOut = strip(root);
            return leftOut == 0 ? json : root.ToString(Formatting.None);
        }

        /// <summary>
        ///     the current skin is built again, so layouts parsed before mods loaded get their components and a faulted mod's go away
        /// </summary>
        public static void Attach(SkinManager manager)
        {
            skins = manager;
            reloadIfUsed(null);
        }

        public static void ModTurnedOff(string modId) => reloadIfUsed(modId);

        internal static void Clear()
        {
            lock (registrations)
                registrations.Clear();

            skins = null;
        }

        private static void reloadIfUsed(string? modId)
        {
            bool used;

            lock (registrations)
                used = registrations.Any(r => modId == null || r.ModId == modId);

            if (used)
                skins?.CurrentSkinInfo.TriggerChange();
        }

        private static int strip(JToken token)
        {
            int count = 0;

            if (token is JArray array)
            {
                for (int i = array.Count - 1; i >= 0; i--)
                {
                    if (array[i] is JObject element && isLeftOut(element))
                    {
                        array.RemoveAt(i);
                        count++;
                    }
                    else
                    {
                        count += strip(array[i]);
                    }
                }
            }
            else if (token is JObject obj)
            {
                foreach (var property in obj.Properties())
                    count += strip(property.Value);
            }

            return count;
        }

        private static bool isLeftOut(JObject element)
        {
            if (element.GetValue("type", StringComparison.OrdinalIgnoreCase) is not JValue { Type: JTokenType.String } value || value.Value<string>() is not { } name)
                return false;

            int comma = name.IndexOf(',', StringComparison.Ordinal);

            if (comma < 0)
                return false;

            string typeName = name[..comma].Trim();
            AssemblyName assembly;

            try
            {
                assembly = new AssemblyName(name[(comma + 1)..].Trim());
            }
            catch (Exception e) when (e is ArgumentException or FileLoadException)
            {
                return false;
            }

            lock (registrations)
            {
                var mine = registrations.FirstOrDefault(r => r.Type.FullName == typeName && r.Type.Assembly.GetName().Name == assembly.Name);

                if (mine.Type != null)
                    return ModFaults.IsFaulted(mine.ModId);
            }

            return !loads(assembly);
        }

        private static bool loads(AssemblyName assembly)
        {
            try
            {
                AssemblyLoadContext.Default.LoadFromAssemblyName(assembly);
                return true;
            }
            catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException)
            {
                return false;
            }
        }
    }
}
