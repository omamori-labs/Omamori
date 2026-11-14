using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Test;
using Cake.Frosting;

namespace Omamori.Build
{
    [TaskName("Test")]
    [TaskDescription("run the test suite, results go to artifacts/test-results")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class TestTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var started = DateTime.UtcNow;

            context.DotNetTest(context.Solution, new DotNetTestSettings
            {
                Configuration = context.BuildConfiguration,
                NoBuild = true,
                NoLogo = true,
                ResultsDirectory = context.TestResults,
                MSBuildSettings = context.MSBuild,
                Loggers = ["trx"],
            });

            // nunit passes a run where it couldn't load the test assembly, so an empty run fails here instead
            var runs = Directory.Exists(context.TestResults)
                ? Directory.GetFiles(context.TestResults, "*.trx").Where(f => File.GetLastWriteTimeUtc(f) >= started).ToList()
                : [];

            if (runs.Count == 0 || runs.Any(f => testCount(f) == 0))
                throw new InvalidOperationException("the test run found no tests, the test assembly probably failed to load (look for a test type deriving from a game type)");
        }

        private static int testCount(string trx) =>
            (int?)XDocument.Load(trx).Descendants().FirstOrDefault(e => e.Name.LocalName == "Counters")?.Attribute("total") ?? 0;
    }
}
