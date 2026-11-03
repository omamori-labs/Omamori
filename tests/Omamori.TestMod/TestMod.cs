using System.Runtime.CompilerServices;
using HarmonyLib;
using Omamori.API;

namespace Omamori.TestMod
{
    public sealed class TestMod : IMod
    {
        public IModContext? Context { get; private set; }

        public void Load(IModContext context)
        {
            Context = context;

            if (context.Manifest.Description == "patches")
                new Harmony(context.Manifest.Id).CreateClassProcessor(typeof(AnswerPatch)).Patch();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Answer() => 1;

        [HarmonyPatch(typeof(TestMod), nameof(Answer))]
        private static class AnswerPatch
        {
            [HarmonyPostfix]
            private static void postfix(ref int __result) => __result = 2;
        }
    }
}
