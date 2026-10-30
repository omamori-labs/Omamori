using System.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Frosting;

namespace Omamori.Build
{
    [TaskName("Pack")]
    [TaskDescription("build the Omamori.ModBuildConfig and Omamori.Templates packages into artifacts/packages")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class PackTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            foreach (string project in new[] { "src/Omamori.ModBuildConfig/Omamori.ModBuildConfig.csproj", "src/Omamori.Templates/Omamori.Templates.csproj" })
            {
                context.DotNetPack(Path.Combine(context.Root, project), new DotNetPackSettings
                {
                    Configuration = context.BuildConfiguration,
                    NoLogo = true,
                    OutputDirectory = context.Packages,
                    MSBuildSettings = context.MSBuild,
                });
            }
        }
    }
}
