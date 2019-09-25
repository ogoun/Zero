using Microsoft.Win32;
using System.Collections;

namespace ZeroLevel.Services.AsService
{
    public class HostInstaller :
        Installer
    {
        readonly string _arguments;
        readonly Installer[] _installers;
        readonly HostSettings _settings;

        public HostInstaller(HostSettings settings, string arguments, Installer[] installers)
        {
            _installers = installers;
            _arguments = arguments;
            _settings = settings;
        }

        public override void Install(IDictionary stateSaver)
        {
            Installers.AddRange(_installers);

            Log.Info("Installing {0} service", _settings.DisplayName);

            base.Install(stateSaver);

            Log.Debug("Open Registry");
            using (RegistryKey system = Registry.LocalMachine.OpenSubKey("System"))
            using (RegistryKey currentControlSet = system.OpenSubKey("CurrentControlSet"))
            using (RegistryKey services = currentControlSet.OpenSubKey("Services"))
            using (RegistryKey service = services.OpenSubKey(_settings.ServiceName, true))
            {
                service.SetValue("Description", _settings.Description);

                var imagePath = (string)service.GetValue("ImagePath");

                Log.Debug("Service path: {0}", imagePath);

                imagePath += _arguments;

                Log.Debug("Image path: {0}", imagePath);

                service.SetValue("ImagePath", imagePath);
            }
            Log.Debug("Closing Registry");
        }

        public override void Uninstall(IDictionary savedState)
        {
            Installers.AddRange(_installers);
            Log.Info("Uninstalling {0} service", _settings.Name);
            base.Uninstall(savedState);
        }
    }
}
