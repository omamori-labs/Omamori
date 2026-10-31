using NUnit.Framework;
using Omamori.Core.ModUi;

namespace Omamori.Tests
{
    [TestFixture]
    public class InputlessContainerTests
    {
        [Test]
        public void PassesNoInputOn()
        {
            var container = new InputlessContainer();

            Assert.That(container.PropagatePositionalInputSubTree, Is.False);
            Assert.That(container.PropagateNonPositionalInputSubTree, Is.False);
        }
    }
}
