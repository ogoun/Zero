using ZeroLevel;
using ZeroLevel.Services.Applications;

namespace ZeroExample
{
    public sealed class MyFirstApp
        : BaseWindowsService, IZeroService
    {
        public MyFirstApp() : base("MyApp")
        {
            Log.AddConsoleLogger();
        }

        public override void PauseAction()
        {
        }

        public override void ResumeAction()
        {
        }

        public override void StartAction()
        {
            Log.Info("Started");
        }

        public override void StopAction()
        {
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Bootstrap.Startup<MyFirstApp>(args);
        }
    }
}