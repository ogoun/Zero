using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroLevel.Services.AsService.Windows
{
    public class WindowsServiceHost :
        ServiceBase,
        Host,
        HostControl
    {
        readonly HostConfigurator _configurator;
        readonly HostEnvironment _environment;
        readonly ServiceHandle _serviceHandle;
        readonly HostSettings _settings;
        int _deadThread;
        bool _disposed;
        Exception _unhandledException;

        public WindowsServiceHost(HostEnvironment environment, HostSettings settings, ServiceHandle serviceHandle, HostConfigurator configurator)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (serviceHandle == null)
                throw new ArgumentNullException(nameof(serviceHandle));

            _settings = settings;
            _serviceHandle = serviceHandle;
            _environment = environment;
            _configurator = configurator;

            CanPauseAndContinue = settings.CanPauseAndContinue;
            CanShutdown = settings.CanShutdown;
            CanHandlePowerEvent = settings.CanHandlePowerEvent;
            CanHandleSessionChangeEvent = settings.CanSessionChanged;
            ServiceName = _settings.ServiceName;
        }

        public ExitCode Run()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            AppDomain.CurrentDomain.UnhandledException += CatchUnhandledException;

            ExitCode = (int)AsService.ExitCode.Ok;

            Log.Info("Starting as a Windows service");

            if (!_environment.IsServiceInstalled(_settings.ServiceName))
            {
                string message = $"The {_settings.ServiceName} service has not been installed yet. Please run '{Assembly.GetEntryAssembly().GetName()} install'.";
                Log.Fatal(message);

                ExitCode = (int)AsService.ExitCode.ServiceNotInstalled;
                throw new Exception(message);
            }

            Log.Debug("[AsService] Starting up as a windows service application");

            Run(this);

            return (ExitCode)Enum.ToObject(typeof(ExitCode), ExitCode);
        }

        void HostControl.RequestAdditionalTime(TimeSpan timeRemaining)
        {
            Log.Debug("Requesting additional time: {0}", timeRemaining);

            RequestAdditionalTime((int)timeRemaining.TotalMilliseconds);
        }

        void HostControl.Stop()
        {
            InternalStop();
        }

        void HostControl.Stop(ExitCode exitCode)
        {
            InternalStop(exitCode);
        }

        void InternalStop(ExitCode? exitCode = null)
        {
            if (CanStop)
            {
                Log.Debug("Stop requested by hosted service");
                if (exitCode.HasValue)
                    ExitCode = (int)exitCode.Value;
                Stop();
            }
            else
            {
                Log.Debug("Stop requested by hosted service, but service cannot be stopped at this time");
                throw new ServiceControlException("The service cannot be stopped at this time");
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Log.Info("[AsService] Starting");

                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                Log.Debug("[AsService] Current Directory: {0}", Directory.GetCurrentDirectory());

                Log.Debug("[AsService] Arguments: {0}", string.Join(",", args));

                string startArgs = string.Join(" ", args);
                _configurator.ApplyCommandLine(startArgs);

                if (!_serviceHandle.Start(this))
                    throw new Exception("The service did not start successfully (returned false).");

                Log.Info("[AsService] Started");
            }
            catch (Exception ex)
            {
                _settings.ExceptionCallback?.Invoke(ex);

                Log.Fatal("The service did not start successfully", ex);

                ExitCode = (int)AsService.ExitCode.ServiceControlRequestFailed;
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                Log.Info("[AsService] Stopping");

                if (!_serviceHandle.Stop(this))
                    throw new Exception("The service did not stop successfully (return false).");

                Log.Info("[AsService] Stopped");
            }
            catch (Exception ex)
            {
                _settings.ExceptionCallback?.Invoke(ex);

                Log.Fatal("The service did not shut down gracefully", ex);
                ExitCode = (int)AsService.ExitCode.ServiceControlRequestFailed;
                throw;
            }

            if (_unhandledException != null)
            {
                ExitCode = (int)AsService.ExitCode.AbnormalExit;
                Log.Info("[AsService] Unhandled exception detected, rethrowing to cause application to restart.");
                throw new InvalidOperationException("An unhandled exception was detected", _unhandledException);
            }
        }

        protected override void OnPause()
        {
            try
            {
                Log.Info("[AsService] Pausing service");

                if (!_serviceHandle.Pause(this))
                    throw new Exception("The service did not pause successfully (returned false).");

                Log.Info("[AsService] Paused");
            }
            catch (Exception ex)
            {
                _settings.ExceptionCallback?.Invoke(ex);

                Log.Fatal("The service did not pause gracefully", ex);
                throw;
            }
        }

        protected override void OnContinue()
        {
            try
            {
                Log.Info("[AsService] Resuming service");

                if (!_serviceHandle.Continue(this))
                    throw new Exception("The service did not continue successfully (returned false).");

                Log.Info("[AsService] Resumed");
            }
            catch (Exception ex)
            {
                _settings.ExceptionCallback?.Invoke(ex);

                Log.Fatal("The service did not resume successfully", ex);
                throw;
            }
        }

        protected override void OnShutdown()
        {
            try
            {
                Log.Info("[AsService] Service is being shutdown");

                _serviceHandle.Shutdown(this);

                Log.Info("[AsService] Stopped");
            }
            catch (Exception ex)
            {
                _settings.ExceptionCallback?.Invoke(ex);

                Log.Fatal("The service did not shut down gracefully", ex);
                ExitCode = (int)AsService.ExitCode.ServiceControlRequestFailed;
                throw;
            }
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            try
            {
                Log.Info("[AsService] Service session changed");

                var arguments = new WindowsSessionChangedArguments(changeDescription);

                _serviceHandle.SessionChanged(this, arguments);

                Log.Info("[AsService] Service session changed handled");
            }
            catch (Exception ex)
            {
                _settings.ExceptionCallback?.Invoke(ex);

                Log.Fatal("The did not handle Service session change correctly", ex);
                ExitCode = (int)AsService.ExitCode.ServiceControlRequestFailed;
                throw;
            }
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            try
            {
                Log.Info("[AsService] Power event raised");

                var arguments = new WindowsPowerEventArguments(powerStatus);

                bool result = _serviceHandle.PowerEvent(this, arguments);

                Log.Info("[AsService] Power event handled");

                return result;
            }
            catch (Exception ex)
            {
                _settings.ExceptionCallback?.Invoke(ex);

                Log.Fatal("The service did handle the Power event correctly", ex);
                ExitCode = (int)AsService.ExitCode.ServiceControlRequestFailed;
                throw;
            }
        }

        protected override void OnCustomCommand(int command)
        {
            try
            {
                Log.Info("[AsService] Custom command {0} received", command);

                _serviceHandle.CustomCommand(this, command);

                Log.Info("[AsService] Custom command {0} processed", command);
            }
            catch (Exception ex)
            {
                _settings.ExceptionCallback?.Invoke(ex);

                Log.Error("Unhandled exception during custom command processing detected", ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _serviceHandle?.Dispose();

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        void CatchUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _settings.ExceptionCallback?.Invoke((Exception)e.ExceptionObject);

            if (_settings.UnhandledExceptionPolicy == UnhandledExceptionPolicyCode.TakeNoAction)
                return;

            Log.Fatal("The service threw an unhandled exception", (Exception)e.ExceptionObject);

            if (_settings.UnhandledExceptionPolicy == UnhandledExceptionPolicyCode.LogErrorOnly)
                return;

            ExitCode = (int)AsService.ExitCode.AbnormalExit;
            _unhandledException = (Exception)e.ExceptionObject;

            Stop();


            // it isn't likely that a TPL thread should land here, but if it does let's no block it
            if (Task.CurrentId.HasValue)
                return;

            int deadThreadId = Interlocked.Increment(ref _deadThread);
            Thread.CurrentThread.IsBackground = true;
            Thread.CurrentThread.Name = "Unhandled Exception " + deadThreadId;
            while (true)
                Thread.Sleep(TimeSpan.FromHours(1));
        }


        class WindowsSessionChangedArguments :
            SessionChangedArguments
        {
            public WindowsSessionChangedArguments(SessionChangeDescription changeDescription)
            {
                ReasonCode = (SessionChangeReasonCode)Enum.ToObject(typeof(SessionChangeReasonCode), (int)changeDescription.Reason);
                SessionId = changeDescription.SessionId;
            }

            public SessionChangeReasonCode ReasonCode { get; }

            public int SessionId { get; }
        }

        class WindowsPowerEventArguments :
            PowerEventArguments
        {
            public WindowsPowerEventArguments(PowerBroadcastStatus powerStatus)
            {
                EventCode = (PowerEventCode)Enum.ToObject(typeof(PowerEventCode), (int)powerStatus);
            }


            public PowerEventCode EventCode { get; }
        }
    }
}
