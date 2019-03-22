using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using ZeroLevel.Services.Applications;

namespace ZeroLevel
{
    public class Bootstrap
    {
        static Bootstrap()
        {
            // Хак, чтобы не переписывать runtime секцию конфига при каждом обновлении Newtonsoft пакета
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
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Bootstrap] Fault load assembly '{args.Name}'");
            }
            return null;
        }

        /// <summary>
        /// Установка приложения в качестве службы
        /// </summary>
        private static void InstallApplication()
        {
            try
            {
                Configuration.Save(Configuration.ReadFromApplicationConfig());
                Log.AddTextFileLogger("install.log");
                BasicServiceInstaller.Install(Configuration.Default);
            }
            catch (Exception ex)
            {
                Log.SystemFatal(ex, "[Bootstrap] Fault service install");
            }
        }
        /// <summary>
        /// Удаление приложения из служб
        /// </summary>
        private static void UninstallApplication()
        {
            try
            {
                Configuration.Save(Configuration.ReadFromApplicationConfig());
                Log.AddTextFileLogger("uninstall.log");
                BasicServiceInstaller.Uninstall(Configuration.Default);
            }
            catch (Exception ex)
            {
                Log.SystemFatal(ex, "[Bootstrap] Fault service uninstall");
            }
        }

        public static void Startup<T>(string[] args, Func<bool> preStartConfiguration = null, Func<bool> postStartConfiguration = null)
            where T : IZeroService, new()
        {
            var cmd = Configuration.ReadFromCommandLine(args);
            if (cmd.Contains("install", "setup"))
            {
                InstallApplication();
            }
            else if (cmd.Contains("uninstall", "remove"))
            {
                UninstallApplication();
            }
            else
            {
                Configuration.Save(Configuration.ReadFromApplicationConfig());
                Log.CreateLoggingFromConfiguration(Configuration.Default);
                IZeroService service = null;
                if (preStartConfiguration != null)
                {
                    try
                    {
                        if (preStartConfiguration() == false)
                        {
                            Log.SystemInfo("[Bootstrap] Service start canceled, because custom preconfig return false");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, "[Bootstrap] Service start canceled, preconfig faulted");
                        return;
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
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, "[Bootstrap] Service start canceled, postconfig faulted");
                        return;
                    }
                }
                // Исключения в процессе работы приложения перехыватываются уровнем ниже
                if (Environment.UserInteractive)
                {
                    try
                    {
                        Log.Debug("[Bootstrap] The service starting (interactive mode)");
                        service?.InteractiveStart(args);
                        Log.Debug("[Bootstrap] The service stopped (interactive mode)");
                    }
                    catch (Exception ex)
                    {
                        Log.SystemFatal(ex, "[Bootstrap] The service start in interactive mode was faulted with error");
                    }
                }
                else
                {
                    try
                    {
                        Log.Debug("[Bootstrap] The service starting (windows service)");
                        ServiceBase.Run(new ServiceBase[] { service as ServiceBase });
                        Log.Debug("[Bootstrap] The service stopped (windows service)");
                    }
                    catch (Exception ex)
                    {
                        Log.SystemFatal(ex, "[Bootstrap] The service start was faulted with error");
                    }
                }
            }
            try { Sheduller.Dispose(); } catch { }
            try { Log.Dispose(); } catch { }
            try { Injector.Default.Dispose(); Injector.Dispose(); } catch { }
        }
    }
}
