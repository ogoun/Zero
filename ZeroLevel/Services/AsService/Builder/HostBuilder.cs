using System;

namespace ZeroLevel.Services.AsService.Builder
{
    public interface HostBuilder
    {
        HostEnvironment Environment { get; }
        HostSettings Settings { get; }

        Host Build(ServiceBuilder serviceBuilder);

        void Match<T>(Action<T> callback)
            where T : class, HostBuilder;
    }
}
