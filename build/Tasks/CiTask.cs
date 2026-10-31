using Cake.Frosting;

namespace Omamori.Build
{
    [TaskName("CI")]
    [TaskDescription("what ci runs: setup, build, test, pack")]
    [IsDependentOn(typeof(TestTask))]
    [IsDependentOn(typeof(PackTask))]
    [IsDependentOn(typeof(ModBuildCheckTask))]
    public sealed class CiTask : FrostingTask<BuildContext>
    {
    }
}
