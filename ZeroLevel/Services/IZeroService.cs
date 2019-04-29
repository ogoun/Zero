namespace ZeroLevel
{
    public interface IZeroService
    {
        ZeroServiceState State { get; }

        string Name { get; }

        void Start();

        void Stop();
    }
}