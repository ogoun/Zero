namespace ZeroLevel.Services.AsService
{
    public static class ServiceControllerExtensions
    {
        public static bool ValidServiceName(string serviceName)
        {
            if (serviceName != null 
                && serviceName.Length <= 80 
                && serviceName.Length != 0)
            {
                return serviceName.IndexOfAny(new[] { '\\', '/' }) == -1;
            }
            return false;
        }
    }
}
