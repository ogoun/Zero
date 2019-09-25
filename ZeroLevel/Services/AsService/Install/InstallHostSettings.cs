namespace ZeroLevel.Services.AsService
{
    public interface InstallHostSettings :
        HostSettings
    {
        Credentials Credentials { get; set; }
        string[] Dependencies { get; }
        HostStartMode StartMode { get; }
    }
}
