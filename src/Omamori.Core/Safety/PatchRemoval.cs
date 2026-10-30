using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;

namespace Omamori.Core
{
    internal static class PatchRemoval
    {
        public static int Remove(IEnumerable<MethodBase> originals, Func<Patch, bool> match)
        {
            int removed = 0;

            foreach (var original in originals.ToList())
            {
                var info = Harmony.GetPatchInfo(original);
                if (info == null)
                    continue;

                foreach (var patch in info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers).Where(match).ToList())
                {
                    try
                    {
                        new Harmony(patch.owner).Unpatch(original, patch.PatchMethod);
                        removed++;
                    }
                    catch (Exception e)
                    {
                        LoaderLog.Error($"Couldn't remove {patch.PatchMethod.DeclaringType?.Name}.{patch.PatchMethod.Name} from {original.Name}.", e);
                    }
                }
            }

            return removed;
        }

        public static int RemoveFrom(AssemblyLoadContext context) =>
            Remove(Harmony.GetAllPatchedMethods(), p => AssemblyLoadContext.GetLoadContext(p.PatchMethod.DeclaringType!.Assembly) == context);
    }
}
