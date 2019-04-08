using System;
using System.ServiceProcess;
using System.Threading;

namespace ZeroLevel.Services.Applications
{
    public abstract class BaseWindowsService
        : ServiceBase, IZeroService
    {
        public string Name { get; protected set; }

        protected BaseWindowsService()
        {
            Name = GetType().Name;
        }

        protected BaseWindowsService(string name)
        {
            Name = name;
        }

        public ZeroServiceState State => _state;
        private ZeroServiceState _state;

        private ManualResetEvent InteraciveModeWorkingFlag = new ManualResetEvent(false);

        public void InteractiveStart(string[] args)
        {
            InteraciveModeWorkingFlag.Reset();
            OnStart(args);
            try
            {
                while (false == InteraciveModeWorkingFlag.WaitOne(2000))
                {
                }
            }
            catch { }
        }

        #region IZeroService

        public abstract void StartAction();

        public abstract void StopAction();

        public abstract void PauseAction();

        public abstract void ResumeAction();

        public abstract void DisposeResources();

        #endregion IZeroService

        #region Windows service

        protected override void OnStart(string[] args)
        {
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
                    Stop();
                }
            }
        }

        protected override void OnPause()
        {
            if (_state == ZeroServiceState.Started)
            {
                try
                {
                    PauseAction();
                    _state = ZeroServiceState.Paused;
                    Log.Debug($"[{Name}] Service paused");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"[{Name}] Failed to pause service");
                    Stop();
                }
            }
        }

        protected override void OnContinue()
        {
            if (_state == ZeroServiceState.Paused)
            {
                try
                {
                    ResumeAction();
                    _state = ZeroServiceState.Started;
                    Log.Debug($"[{Name}] Service continue work after pause");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"[{Name}] Failed to continue work service after pause");
                    Stop();
                }
            }
        }

        protected override void OnStop()
        {
            if (_state != ZeroServiceState.Stopped)
            {
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
                finally
                {
                    InteraciveModeWorkingFlag?.Set();
                }
            }
        }

        #endregion Windows service
    }
}