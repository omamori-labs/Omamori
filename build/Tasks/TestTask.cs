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
        public override void Run(BuildContext context) =>
            context.DotNetTest(context.Solution, new DotNetTestSettings
            {
                Configuration = context.BuildConfiguration,
                NoBuild = true,
                NoLogo = true,
                ResultsDirectory = context.TestResults,
                MSBuildSettings = context.MSBuild,
                Loggers = ["trx"],
            });
    }
}
