using System.Threading.Tasks;
using Cake.Frosting;

namespace Omamori.Build
{
    [TaskName("SetupSandbox")]
    [TaskDescription("download, verify and extract the lazer release from build/lazer-version.txt into sandbox/lazer")]
    public sealed class SetupSandboxTask : AsyncFrostingTask<BuildContext>
    {
        public override Task RunAsync(BuildContext context) => LazerRelease.SetupAsync(context);
    }
}
