namespace ZeroLevel.Services.Applications
{
    public interface IZeroService
    {
        ZeroServiceState State { get; }

        void StartAction();

        void StopAction();

        void PauseAction();

        void ResumeAction();

        void InteractiveStart(string[] args);
    }
}