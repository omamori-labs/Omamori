using NUnit.Framework;
using Omamori.API;
using osu.Framework.Bindables;
using osu.Framework.Input.Bindings;

namespace Omamori.Tests
{
    [TestFixture]
    public class ModKeysTests
    {
        [Test]
        public void OrderDoesNotMatter() => Assert.That(new ModKeys(InputKey.K, InputKey.Control), Is.EqualTo(new ModKeys(InputKey.Control, InputKey.K)));

        [Test]
        public void NoKeysIsNone() => Assert.That(new ModKeys(InputKey.None), Is.EqualTo(ModKeys.NONE));

        [Test]
        public void WorksInABindable() => Assert.DoesNotThrow(() => _ = new Bindable<ModKeys>(new ModKeys(InputKey.K)) { Value = ModKeys.NONE });

        [Test]
        public void RoundTripsThroughLazersCombination() =>
            Assert.That(ModKeys.From(new ModKeys(InputKey.Shift, InputKey.F2).ToCombination()), Is.EqualTo(new ModKeys(InputKey.Shift, InputKey.F2)));

        [Test]
        public void NoneBecomesLazersNone() => Assert.That(ModKeys.NONE.ToCombination(), Is.EqualTo(new KeyCombination(InputKey.None)));
    }
}
