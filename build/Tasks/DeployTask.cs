using System.IO;
using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Frosting;

namespace Omamori.Build
{
    [TaskName("Deploy")]
    [TaskDescription("install the staged build into the sandbox, plus every mod project under each --mods folder")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class DeployTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            string rulesets = Path.Combine(context.Sandbox, "rulesets");
            Directory.CreateDirectory(rulesets);
            foreach (string file in Directory.EnumerateFiles(Path.Combine(context.Stage, "rulesets")))
                File.Copy(file, Path.Combine(rulesets, Path.GetFileName(file)), overwrite: true);

            mirror(Path.Combine(context.Stage, "omamori", "core"), Path.Combine(context.Sandbox, "omamori", "core"));

            foreach (string source in context.ModSources.Where(Directory.Exists))
            {
                foreach (string project in Directory.EnumerateFiles(source, "*.csproj", SearchOption.AllDirectories))
                    deployMod(context, project);
            }

            context.Information($"deployed to {context.Sandbox}");
        }

        private static void deployMod(BuildContext context, string project)
        {
            context.DotNetBuild(project, new DotNetBuildSettings { Configuration = context.BuildConfiguration, NoLogo = true, MSBuildSettings = context.MSBuild });

            string name = Path.GetFileNameWithoutExtension(project);
            string output = Path.Combine(Path.GetDirectoryName(project)!, "bin", context.BuildConfiguration, "net10.0");
            string target = Path.Combine(context.Sandbox, "omamori", "mods", name.ToLowerInvariant());

            mirror(output, target);

            context.Information($"  mod {name}");
        }

        private static void mirror(string source, string target)
        {
            if (Directory.Exists(target))
                Directory.Delete(target, recursive: true);
            Directory.CreateDirectory(target);

            foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                string destination = Path.Combine(target, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination);
            }
        }
    }
}
