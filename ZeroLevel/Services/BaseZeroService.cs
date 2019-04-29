using System;
using System.Threading;

namespace ZeroLevel.Services.Applications
{
    public abstract class BaseZeroService
        : IZeroService
    {
        public string Name { get; protected set; }
        public ZeroServiceState State => _state;
        private ZeroServiceState _state;

        protected BaseZeroService()
        {
            Name = GetType().Name;
        }

        protected BaseZeroService(string name)
        {
            Name = name;
        }

        private ManualResetEvent InteraciveModeWorkingFlag = new ManualResetEvent(false);

        protected abstract void StartAction();
        protected abstract void StopAction();

        public void Start()
        {
            InteraciveModeWorkingFlag.Reset();
            if (_state == ZeroServiceState.Initialized)
            {
                try
                {
                    StartAction();
                    _state = ZeroServiceState.Started;
                    Log.Debug($"[{Name}] Service started");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"[{Name}] Failed to start service");
                    InteraciveModeWorkingFlag.Set();
                }
            }
            try
            {
                while (false == InteraciveModeWorkingFlag.WaitOne(2000))
                {
                }
            }
            catch { }
            _state = ZeroServiceState.Stopped;
            try
            {
                StopAction();
                Log.Debug($"[{Name}] Service stopped");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"[{Name}] Failed to stop service");
            }
        }

        public void Stop()
        {
            InteraciveModeWorkingFlag.Set();
        }
    }
}