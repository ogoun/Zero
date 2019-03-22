namespace ZeroLevel.Services.Config
{
    public interface IConfigurationReader
    {
        IConfiguration ReadConfiguration();
        IConfigurationSet ReadConfigurationSet();
    }
}
