using ZeroLevel;

namespace Semantic.API
{
    static class Program
    {
        static void Main(params string[] args)
        {
            Bootstrap.Startup<HostService>(args, () => { Log.Backlog(100); return true; });
        }
    }
}
