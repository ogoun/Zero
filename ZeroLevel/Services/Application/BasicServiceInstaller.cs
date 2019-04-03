using System;
using System.Collections;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ZeroLevel.Services.Applications
{
    internal static class BasicServiceInstaller
    {
        private class InstallOptions
        {
            public string ServiceName;
            public string ServiceDisplayName;
            public string ServiceDescription;
            public ServiceStartMode ServiceStartType = ServiceStartMode.Automatic;
            public ServiceAccount ServiceAccountType = ServiceAccount.LocalSystem;
            public string ServiceUserName;
            public string ServiceUserPassword;
        }

        private static InstallOptions ReadOptions(IConfiguration configuration)
        {
            if (configuration == null)
            {
                configuration = Configuration.Default;
            }
            var options = new InstallOptions();
            if (configuration.Contains("ServiceDescription"))
            {
                options.ServiceDescription = configuration.First("ServiceDescription");
            }
            if (configuration.Contains("ServiceName"))
            {
                options.ServiceName = configuration.First("ServiceName");
            }
            if (configuration.Contains("ServiceDisplayName"))
            {
                options.ServiceDisplayName = configuration.First("ServiceDisplayName");
            }
            else
            {
                options.ServiceDisplayName = options.ServiceName;
            }
            if (configuration.Contains("ServiceUserName"))
            {
                options.ServiceUserName = configuration.First("ServiceUserName");
            }
            if (configuration.Contains("ServiceUserPassword"))
            {
                options.ServiceUserPassword = configuration.First("ServiceUserPassword");
            }

            if (configuration.Contains("ServiceStartType"))
            {
                var startType = configuration.First("ServiceStartType");
                ServiceStartMode mode;
                if (Enum.TryParse(startType, out mode))
                {
                    options.ServiceStartType = mode;
                }
                else
                {
                    options.ServiceStartType = ServiceStartMode.Automatic;
                }
            }
            if (configuration.Contains("ServiceAccountType"))
            {
                var accountType = configuration.First("ServiceAccountType");
                ServiceAccount type;
                if (Enum.TryParse(accountType, out type))
                {
                    options.ServiceAccountType = type;
                }
                else
                {
                    options.ServiceAccountType = ServiceAccount.LocalService;
                }
            }
            return options;
        }

        public static void Install(IConfiguration configuration)
        {
            CreateInstaller(ReadOptions(configuration)).Install(new Hashtable());
        }

        public static void Uninstall(IConfiguration configuration)
        {
            CreateInstaller(ReadOptions(configuration)).Uninstall(null);
        }

        private static Installer CreateInstaller(InstallOptions options)
        {
            var installer = new TransactedInstaller();
            installer.Installers.Add(new ServiceInstaller()
            {
                ServiceName = options.ServiceName,
                DisplayName = options.ServiceDisplayName,
                StartType = options.ServiceStartType,
                Description = options.ServiceDescription
            });
            installer.Installers.Add(new ServiceProcessInstaller
            {
                Account = options.ServiceAccountType,
                Username = (options.ServiceAccountType == ServiceAccount.User) ? options.ServiceUserName : null,
                Password = (options.ServiceAccountType == ServiceAccount.User) ? options.ServiceUserPassword : null
            });
            var installContext = new InstallContext(options.ServiceName + ".install.log", null);
            installContext.Parameters["assemblypath"] = Configuration.AppLocation;
            installer.Context = installContext;
            return installer;
        }
    }
}