using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Frosting;

namespace Omamori.Build
{
    [TaskName("Build")]
    [TaskDescription("build the solution, which also stages artifacts/stage/<configuration>")]
    [IsDependentOn(typeof(SetupSandboxTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context) =>
            context.DotNetBuild(context.Solution, new DotNetBuildSettings { Configuration = context.BuildConfiguration, NoLogo = true, MSBuildSettings = context.MSBuild });
    }
}
