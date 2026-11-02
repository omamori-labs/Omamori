using Omamori.API;

namespace Omamori.TestMod
{
    public sealed class TestMod : IMod
    {
        public IModContext? Context { get; private set; }

        public void Load(IModContext context) => Context = context;
    }
}
