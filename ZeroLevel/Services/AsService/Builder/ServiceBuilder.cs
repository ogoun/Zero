namespace ZeroLevel.Services.AsService.Builder
{
    public interface ServiceBuilder
    {
        ServiceHandle Build(HostSettings settings);
    }
}
