using ZeroLevel.Services.Applications;

namespace Semantic.API
{
    public class HostService :
        BaseZeroService
    {
        public HostService() : base("Semantic api service")
        {
        }

        protected override void StartAction()
        {
            // Запуск web API
            Startup.Run(false, false);
        }

        protected override void StopAction()
        {
        }
    }
}
