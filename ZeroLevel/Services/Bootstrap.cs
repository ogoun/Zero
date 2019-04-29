using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ZeroLevel
{
    public static class Bootstrap
    {
        static Bootstrap()
        {
            // Tricks for minimize config changes for dependency resolve
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                Log.Debug($"[Bootstrap] Resolve assembly '{args.Name}'");
                if (args.Name.StartsWith("Newtonsoft.Json", StringComparison.Ordinal))
                {
                    return Assembly.LoadFile(Path.Combine(Configuration.BaseDirectory, "Newtonsoft.Json.dll"));
                }
                else if (args.Name.Equals("Microsoft.Owin", StringComparison.Ordinal))
                {
                    return Assembly.LoadFile(Path.Combine(Configuration.BaseDirectory, "Microsoft.Owin.dll"));
                }
                var candidates = Directory.GetFiles(Path.Combine(Configuration.BaseDirectory), args.Name, SearchOption.TopDirectoryOnly);
                if (candidates != null && candidates.Any())
                {
                    return Assembly.LoadFile(candidates.First());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Bootstrap] Fault load assembly '{args.Name}'");
            }
            return null;
        }

        public static void Startup<T>(string[] args, Func<bool> preStartConfiguration = null, Func<bool> postStartConfiguration = null)
            where T : IZeroService, new()
        {
            var service = Initialize<T>(args, preStartConfiguration, postStartConfiguration);
            if (service != null)
            {
                service.Start();
                Shutdown(service);
            }
        }

        private static IZeroService Initialize<T>(string[] args, Func<bool> preStartConfiguration = null, Func<bool> postStartConfiguration = null)
            where T : IZeroService, new()
        {
            IZeroService service = null;

            Configuration.Save(Configuration.ReadFromApplicationConfig());
            Log.CreateLoggingFromConfiguration(Configuration.Default);
            if (preStartConfiguration != null)
            {
                try
                {
                    if (preStartConfiguration() == false)
                    {
                        Log.SystemInfo("[Bootstrap] Service start canceled, because custom preconfig return false");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[Bootstrap] Service start canceled, preconfig faulted");
                    return null;
                }
            }
            try
            {
                service = new T();
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

        private static void Shutdown(IZeroService service)
        {
            try { Sheduller.Dispose(); } catch (Exception ex) { Log.Error(ex, "[Bootstrap] Dispose default sheduller error"); }
            try { Log.Dispose(); } catch (Exception ex) { Log.Error(ex, "[Bootstrap] Dispose log error"); }
            try { Injector.Default.Dispose(); Injector.Dispose(); } catch (Exception ex) { Log.Error(ex, "[Bootstrap] Dispose DI containers error"); }
            try { (service as IDisposable)?.Dispose(); } catch (Exception ex) { Log.Error(ex, $"[Bootstrap] Service {service?.Name} dispose error"); }
        }
    }
}