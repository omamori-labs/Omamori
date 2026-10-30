using Cake.Frosting;

namespace Omamori.Build
{
    [TaskName("Default")]
    [IsDependentOn(typeof(TestTask))]
    public sealed class DefaultTask : FrostingTask<BuildContext>
    {
    }
}
