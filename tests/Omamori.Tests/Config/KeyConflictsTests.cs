using System.Collections.Generic;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Config;
using osu.Framework.Input.Bindings;

namespace Omamori.Tests
{
    [TestFixture]
    public class KeyConflictsTests
    {
        private static readonly ModKeys ctrl_k = new ModKeys(InputKey.Control, InputKey.K);

        [Test]
        public void SameKeysConflict() =>
            Assert.That(find(ctrl_k, new KeyUse("osu!: Toggle chat", ctrl_k)), Is.EqualTo(new List<string> { "osu!: Toggle chat" }));

        [Test]
        public void OneSidedModifierStillConflicts() =>
            Assert.That(find(ctrl_k, new KeyUse("osu!mania 4K gameplay: Key 1", new ModKeys(InputKey.LControl, InputKey.K))), Has.Count.EqualTo(1));

        [Test]
        public void ExtraModifiersDoNotConflict() =>
            Assert.That(find(new ModKeys(InputKey.K), new KeyUse("osu!: Toggle chat", ctrl_k)), Is.Empty);

        [Test]
        public void UnboundNeverConflicts() => Assert.That(find(ModKeys.NONE, new KeyUse("Other", ModKeys.NONE)), Is.Empty);

        [Test]
        public void TheSettingItselfIsSkipped() =>
            Assert.That(find(ctrl_k, new KeyUse("Mine: Toggle", ctrl_k, ("test.mine", "toggle")), new KeyUse("Mine: Other", ctrl_k, ("test.mine", "other"))),
                Is.EqualTo(new List<string> { "Mine: Other" }));

        [Test]
        public void LongListsAreCut() =>
            Assert.That(KeyConflicts.Note(["a", "b", "c", "d", "e"]), Is.EqualTo("Also used by a, b, c and 2 more."));

        [Test]
        public void NoConflictsNoNote() => Assert.That(KeyConflicts.Note([]), Is.Null);

        private static IReadOnlyList<string> find(ModKeys keys, params KeyUse[] uses) => KeyConflicts.Find(keys, "test.mine", "toggle", uses);
    }
}
