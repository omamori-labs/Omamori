using System;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Services;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class ModServicesTests
    {
        [TearDown]
        public void TearDown() => ModServices.Clear();

        [Test]
        public void AnotherModGetsWhatWasProvided()
        {
            var greeter = new Greeter();
            services("test.provider").Provide<IGreeter>(greeter);

            Assert.That(services("test.consumer").Get<IGreeter>(), Is.SameAs(greeter));
        }

        [Test]
        public void NothingProvidedIsNull() => Assert.That(services("test.consumer").Get<IGreeter>(), Is.Null);

        [Test]
        public void TheFirstProviderWins()
        {
            var first = new Greeter();
            services("test.first").Provide<IGreeter>(first);
            services("test.second").Provide<IGreeter>(new Greeter());

            Assert.That(services("test.consumer").Get<IGreeter>(), Is.SameAs(first));
        }

        [Test]
        public void ProvidingTheSameTypeTwiceThrows()
        {
            var provider = services("test.provider");
            provider.Provide<IGreeter>(new Greeter());

            Assert.Throws<InvalidOperationException>(() => provider.Provide<IGreeter>(new Greeter()));
        }

        private static ModServices services(string id) => new ModServices(new ModManifest { Id = id }, Substitute.For<IModLogger>());

        public interface IGreeter
        {
            string Greet();
        }

        private sealed class Greeter : IGreeter
        {
            public string Greet() => "hi";
        }
    }
}
