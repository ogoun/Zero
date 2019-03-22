namespace ZeroLevel.Services.Config
{
    public interface IConfigurationWriter
    {
        void WriteConfiguration(IConfiguration configuration);
        void WriteConfigurationSet(IConfigurationSet configuration);
    }
}
