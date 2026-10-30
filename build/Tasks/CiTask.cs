using Cake.Frosting;

namespace Omamori.Build
{
    [TaskName("CI")]
    [TaskDescription("what ci runs: setup, build, test, pack")]
    [IsDependentOn(typeof(TestTask))]
    [IsDependentOn(typeof(PackTask))]
    public sealed class CiTask : FrostingTask<BuildContext>
    {
    }
}
