using System;
using System.IO;
using System.Net;
using System.Reflection;
using ZeroLevel.Logging;
using ZeroLevel.Network;

namespace ZeroLevel
{
    public static class Bootstrap
    {
        public interface IServiceExecution
        {
            IServiceExecution Run();
            IServiceExecution WaitForStatus(ZeroServiceStatus status);
            IServiceExecution WaitForStatus(ZeroServiceStatus status, TimeSpan period);
            IServiceExecution WaitWhileStatus(ZeroServiceStatus status);
            IServiceExecution WaitWhileStatus(ZeroServiceStatus status, TimeSpan period);
            IServiceExecution Stop();
            IZeroService Service { get; }
            ZeroServiceStatus Status { get; }
        }

        public class BootstrapFluent
            : IServiceExecution
        {
            private readonly IZeroService _service;
            public IZeroService Service { get { return _service; } }

            public BootstrapFluent(IZeroService service)
            {
                if (service == null)
                {
                    throw new ArgumentNullException(nameof(service));
                }
                _service = service;
            }

            public BootstrapFluent UseDiscovery() { _service?.UseDiscovery(); return this; }
            public BootstrapFluent UseDiscovery(string url) { _service?.UseDiscovery(url); return this; }
            public BootstrapFluent UseDiscovery(IPEndPoint endpoint) { _service?.UseDiscovery(endpoint); return this; }

            public BootstrapFluent ReadServiceInfo() { _service?.ReadServiceInfo(); return this; }
            public BootstrapFluent ReadServiceInfo(IConfigurationSet config) { _service?.ReadServiceInfo(config); return this; }

            public BootstrapFluent EnableConsoleLog(LogLevel level = LogLevel.FullStandart) { Log.AddConsoleLogger(level); return this; }

            public ZeroServiceStatus Status { get { return _service.Status; } }

            public IServiceExecution Run()
            {
                try
                {
                    _service?.Start();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"[Bootstrap] Service {_service?.Name} run error");
                    return null;
                }
                return this;
            }

            public IServiceExecution Stop()
            {
                try
                {
                    _service?.Stop();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"[Bootstrap] Service {_service?.Name} dispose error");
                }
                return this;
            }

            public IServiceExecution WaitForStatus(ZeroServiceStatus status)
            {
                _service.WaitForStatus(status);
                return this;
            }
            public IServiceExecution WaitForStatus(ZeroServiceStatus status, TimeSpan period)
            {
                _service.WaitForStatus(status, period);
                return this;
            }
            public IServiceExecution WaitWhileStatus(ZeroServiceStatus status)
            {
                _service.WaitWhileStatus(status);
                return this;
            }
            public IServiceExecution WaitWhileStatus(ZeroServiceStatus status, TimeSpan period)
            {
                _service.WaitWhileStatus(status, period);
                return this;
            }
        }

        static Bootstrap()
        {
            // Tricks for minimize config changes for dependency resolve
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                Log.Info($"[Bootstrap] Resolve assembly '{args.Name}' {args.Name}");
                string name = args.Name.Split(',')[0];
                if (name.EndsWith(".resources")) return null;
                foreach (string file in Directory.
                    GetFiles(Path.Combine(Configuration.BaseDirectory), "*.dll", SearchOption.TopDirectoryOnly))
                {
                    if (file.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Log.Info($"[Bootstrap] Resolve assembly '{args.Name}' found '{file}'");
                        return Assembly.LoadFile(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Bootstrap] Fault load assembly '{args.Name}'");
            }
            return null;
        }

        public static BootstrapFluent Startup<T>(string[] args,
            Func<bool> preStartConfiguration = null!,
            Func<bool> postStartConfiguration = null!)
            where T : IZeroService
        {
            var service = Initialize<T>(args, null!,
                preStartConfiguration, postStartConfiguration);
            return new BootstrapFluent(service);
        }

        public static BootstrapFluent Startup<T>(string[] args,
            Func<IConfigurationSet> configuration,
            Func<bool> preStartConfiguration = null!,
            Func<bool> postStartConfiguration = null!)
            where T : IZeroService
        {
            var service = Initialize<T>(args, configuration?.Invoke()!, preStartConfiguration, postStartConfiguration);
            return new BootstrapFluent(service);
        }

        private static IZeroService Initialize<T>(string[] args,
            IConfigurationSet configurationSet,
            Func<bool> preStartConfiguration = null!,
            Func<bool> postStartConfiguration = null!)
            where T : IZeroService
        {
            IZeroService service = null!;
            IConfigurationSet config = Configuration.DefaultSet;
            config.CreateSection("commandline", Configuration.ReadFromCommandLine(args));
            if (configurationSet != null)
            {
                config.Merge(configurationSet);
            }
            Log.CreateLoggingFromConfiguration(Configuration.DefaultSet);

            if (preStartConfiguration != null)
            {
                try
                {
                    if (preStartConfiguration() == false)
                    {
                        Log.SystemInfo("[Bootstrap] Service start canceled, because custom preconfig return false");
                        return null!;
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[Bootstrap] Service start canceled, preconfig faulted");
                    return null!;
                }
            }
            try
            {
                service = Activator.CreateInstance<T>();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[Bootstrap] Service start canceled, service constructor call fault");
            }
            if (postStartConfiguration != null)
            {
                try
                {
                    if (postStartConfiguration() == false)
                    {
                        Log.SystemInfo("[Bootstrap] Service start canceled, because custom postconfig return false");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[Bootstrap] Service start canceled, postconfig faulted");
                    return null;
                }
            }
            return service;
        }

        public static IExchange CreateExchange() => new Exchange(null!);

        public static void Shutdown()
        {
            try { Sheduller.Dispose(); } catch (Exception ex) { Log.Error(ex, "[Bootstrap] Dispose default sheduller error"); }
            try { Log.Dispose(); } catch (Exception ex) { Log.Error(ex, "[Bootstrap] Dispose log error"); }
            try { Injector.Default.Dispose(); Injector.Dispose(); } catch (Exception ex) { Log.Error(ex, "[Bootstrap] Dispose DI containers error"); }
        }
    }
}