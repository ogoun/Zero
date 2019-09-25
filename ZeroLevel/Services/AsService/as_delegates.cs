using ZeroLevel.Services.AsService.Builder;

namespace ZeroLevel.Services.AsService
{
    public delegate HostBuilder HostBuilderFactory(HostEnvironment environment, HostSettings settings);
    public delegate ServiceBuilder ServiceBuilderFactory(HostSettings settings);
    public delegate EnvironmentBuilder EnvironmentBuilderFactory(HostConfigurator configurator);
}
