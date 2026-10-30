using Cake.Frosting;

namespace Omamori.Build
{
    public static class Program
    {
        public static int Main(string[] args) => new CakeHost().UseContext<BuildContext>().Run(args);
    }
}
