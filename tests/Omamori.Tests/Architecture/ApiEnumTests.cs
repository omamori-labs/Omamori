using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Omamori.API;
using static VerifyNUnit.Verifier;

namespace Omamori.Tests
{
    [TestFixture]
    public class ApiEnumTests
    {
        [Test]
        public Task ValuesNeverMove() => Verify(typeof(IMod).Assembly.GetExportedTypes()
                                                            .Where(t => t.IsEnum)
                                                            .ToDictionary(t => t.Name, t => Enum.GetValues(t).Cast<Enum>()
                                                                                                .ToDictionary(v => v.ToString(), v => Convert.ToInt64(v, CultureInfo.InvariantCulture))));
    }
}
