using System.Collections;

namespace ZeroLevel.Services.AsService
{
    public delegate void InstallEventHandler(object sender, InstallEventArgs e);

    public class InstallEventArgs
    {
        public IDictionary SavedSate { get; }

        public InstallEventArgs()
        {
        }

        public InstallEventArgs(IDictionary savedSate)
        {
            this.SavedSate = savedSate;
        }
    }
}
