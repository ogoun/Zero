using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ZeroLevel.Services.Logging
{
    internal sealed class LogRouter : 
        ILogger, 
        ILogComposer
    {
        #region Fields
        private long _backlog = -1;
        private volatile bool _stopped;
        private readonly object LogsCacheeLocker = new object();
        private readonly Dictionary<int, List<ILogger>> LogWriters = new Dictionary<int, List<ILogger>>();
        private volatile ILogMessageBuffer _messageQueue;
        #endregion

        #region  Ctor
        public LogRouter()
        {
            _messageQueue = new NoLimitedLogMessageBuffer();
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => { Dispose(); };
            _stopped = false;
            var thread = new Thread(ProcessMessageQueueMethod) { IsBackground = true };
            thread.Start();
        }
        #endregion

        #region Routing
        public void SetupBacklog(long backlog)
        {
            if (backlog != _backlog)
            {
                var currentQueue = _messageQueue;
                if (backlog > 0) // Fix
                {
                    _messageQueue = new FixSizeLogMessageBuffer(backlog);
                }
                else // Unlimited
                {
                    _messageQueue = new NoLimitedLogMessageBuffer();
                }
                while (currentQueue.Count > 0)
                {
                    var t = currentQueue.Take();
                    _messageQueue.Push(t.Item1, t.Item2);
                }
                currentQueue.Dispose();
                currentQueue = null;
                GC.Collect();
                GC.WaitForFullGCComplete();
            }
        }

        private void ProcessMessageQueueMethod(object state)
        {
            while (false == _stopped || _messageQueue.Count > 0)
            {
                var message = _messageQueue.Take();
                if (message != null)
                {
                    lock (LogsCacheeLocker)
                    {
                        foreach (var lv in LogWriters.Keys)
                        {
                            if ((lv & ((int)message.Item1)) != 0)
                            {
                                foreach (var logger in LogWriters[lv])
                                {
                                    logger.Write(message.Item1, message.Item2);
                                }
                            }
                        }
                    }
                    message = null;
                }
            }
        }

        public void AddLogger(ILogger logger, LogLevel level = LogLevel.All)
        {
            if (false == _stopped)
            {
                lock (LogsCacheeLocker)
                {
                    var lv = (int)level;
                    if (false == LogWriters.ContainsKey(lv))
                    {
                        LogWriters.Add(lv, new List<ILogger>());
                    }
                    LogWriters[lv].Add(logger);
                }
            }
        }
        #endregion

        #region ILogger
        public void Write(LogLevel level, string message)
        {
            if (false == _stopped)
            {
                _messageQueue.Push(level, message);
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (false == _stopped)
            {
                _stopped = true;
                while (_messageQueue.Count > 0)
                {
                    Thread.Sleep(100);
                }
                _messageQueue.Dispose();
                lock (LogsCacheeLocker)
                {
                    foreach (var logCollection in LogWriters.Values)
                    {
                        foreach (var logger in logCollection)
                        {
                            try
                            {
                                logger.Dispose();
                            }
                            catch { }
                        }
                    }
                    LogWriters.Clear();
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        #endregion
    }
}
