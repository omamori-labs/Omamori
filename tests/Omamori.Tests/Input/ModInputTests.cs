using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Input;
using osu.Framework.Bindables;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.States;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class ModInputTests
    {
        private static readonly InputState state = new InputState();

        private ModInput input = null!;
        private List<string> calls = null!;

        [SetUp]
        public void SetUp()
        {
            FeatureRegistry.Set(Feature.KeyBindings, FeatureState.Active, null);
            input = new ModInput(new ModManifest { Id = "test.input", Hooks = new Dictionary<Hook, HookRequirement> { [Hook.KeyBindings] = HookRequirement.Optional } },
                Substitute.For<IModLogger>());
            calls = [];
        }

        [TearDown]
        public void TearDown()
        {
            ModInput.Clear();
            FeatureRegistry.Set(Feature.KeyBindings, FeatureState.Pending, null);
        }

        [Test]
        public void PressAndReleaseFireOnce()
        {
            listen(new Bindable<ModKeys>(new ModKeys(InputKey.K)));

            press(InputKey.K);
            press(InputKey.K);
            press();

            Assert.That(calls, Is.EqualTo(new List<string> { "down", "up" }));
        }

        [Test]
        public void ModifiersMatchExactly()
        {
            listen(new Bindable<ModKeys>(new ModKeys(InputKey.Control, InputKey.K)));

            press(InputKey.K);
            press(InputKey.LControl, InputKey.LShift, InputKey.K);

            Assert.That(calls, Is.Empty);

            press(InputKey.RControl, InputKey.K);

            Assert.That(calls, Is.EqualTo(new List<string> { "down" }));
        }

        [Test]
        public void FiresWhileTappingHeld()
        {
            listen(new Bindable<ModKeys>(new ModKeys(InputKey.Control, InputKey.K)));

            press(InputKey.MouseLeft, InputKey.Z, InputKey.LControl, InputKey.K);

            Assert.That(calls, Is.EqualTo(new List<string> { "down" }));
        }

        [Test]
        public void UnboundNeverFires()
        {
            listen(new Bindable<ModKeys>(ModKeys.NONE));

            press();
            press(InputKey.K);

            Assert.That(calls, Is.Empty);
        }

        [Test]
        public void FollowsARebind()
        {
            var keys = new Bindable<ModKeys>(new ModKeys(InputKey.K));
            listen(keys);

            keys.Value = new ModKeys(InputKey.J);
            press(InputKey.K);
            press(InputKey.J);

            Assert.That(calls, Is.EqualTo(new List<string> { "down" }));
        }

        [Test]
        public void UnavailableHookSkipsTheHandler()
        {
            FeatureRegistry.Set(Feature.KeyBindings, FeatureState.Pending, null);
            listen(new Bindable<ModKeys>(new ModKeys(InputKey.K)));

            press(InputKey.K);

            Assert.That(calls, Is.Empty);
        }

        private void listen(IBindable<ModKeys> keys)
        {
            input.OnPressed(keys, () => calls.Add("down"));
            input.OnReleased(keys, () => calls.Add("up"));
        }

        private static void press(params InputKey[] keys) => ModInput.KeysChanged(new KeyCombination(keys), state);
    }
}
