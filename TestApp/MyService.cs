using System;
using ZeroLevel;
using ZeroLevel.Services.Applications;

namespace TestApp
{
    public class MyService
        : BaseZeroService
    {
        protected override void StartAction()
        {
            Log.Info("Started");
            Sheduller.RemindEvery(TimeSpan.FromSeconds(5), () => Log.Info("Still alive"));
        }

        protected override void StopAction()
        {
            Log.Info("Stopped");
        }
    }
}
