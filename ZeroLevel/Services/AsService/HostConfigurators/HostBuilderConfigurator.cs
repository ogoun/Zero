using ZeroLevel.Services.AsService.Builder;

namespace ZeroLevel.Services.AsService
{
    public interface HostBuilderConfigurator :
        Configurator
    {
        /// <summary>
        /// Configures the host builder.
        /// </summary>
        /// <param name="builder">The host builder.</param>
        /// <returns>The configured host builder.</returns>
        HostBuilder Configure(HostBuilder builder);
    }
}
