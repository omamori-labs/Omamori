using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Config;
using osu.Framework.Input.Bindings;

namespace Omamori.Tests
{
    [TestFixture]
    public class KeyCaptureTests
    {
        [Test]
        public void AKeyBinds() => Assert.That(KeyCapture.Decide(InputKey.K, new KeyCombination(InputKey.K)), Is.EqualTo(KeyCaptureResult.Bind));

        [Test]
        public void AModifierWaitsForItsKey() =>
            Assert.That(KeyCapture.Decide(InputKey.LControl, new KeyCombination(InputKey.Control, InputKey.LControl)), Is.EqualTo(KeyCaptureResult.Waiting));

        [Test]
        public void EscapeAloneCancels() => Assert.That(KeyCapture.Decide(InputKey.Escape, new KeyCombination(InputKey.Escape)), Is.EqualTo(KeyCaptureResult.Cancel));

        [Test]
        public void DeleteAloneClears() => Assert.That(KeyCapture.Decide(InputKey.Delete, new KeyCombination(InputKey.Delete)), Is.EqualTo(KeyCaptureResult.Clear));

        [Test]
        public void ShiftDeleteBinds() =>
            Assert.That(KeyCapture.Decide(InputKey.Delete, new KeyCombination(InputKey.LShift, InputKey.Delete)), Is.EqualTo(KeyCaptureResult.Bind));

        [Test]
        public void CapturedModifiersWorkOnEitherSide() =>
            Assert.That(KeyCapture.Combination(new KeyCombination(InputKey.RControl, InputKey.K), InputKey.K), Is.EqualTo(new ModKeys(InputKey.Control, InputKey.K)));

        [Test]
        public void OtherHeldKeysAreLeftOut() =>
            Assert.That(KeyCapture.Combination(new KeyCombination(InputKey.MouseLeft, InputKey.J, InputKey.K), InputKey.K), Is.EqualTo(new ModKeys(InputKey.K)));
    }
}
