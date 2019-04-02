using System;
using System.Globalization;
using System.IO;
using System.Text;
using ZeroLevel.Services.Encryption;

namespace ZeroLevel.Services.Logging.Implementation
{
    public class EncryptedFileLogOptions
    {
        public EncryptedFileLogOptions()
        {
            Folder = null;
            LimitFileSize = 0;
        }
        internal string Key { get; private set; }
        internal string Folder { get; private set; }
        internal long LimitFileSize { get; private set; }

        public EncryptedFileLogOptions SetKey(string key)
        {
            this.Key = key;
            return this;
        }

        public EncryptedFileLogOptions SetFolderPath(string folder)
        {
            if (folder.IndexOf(':') < 0)
            {
                this.Folder = Path.Combine(Configuration.BaseDirectory, folder);
            }
            else
            {
                this.Folder = folder;
            }
            return this;
        }

        public EncryptedFileLogOptions SetMaximumFileSizeInKb(long size)
        {
            this.LimitFileSize = size;
            return this;
        }
    }

    public class EncryptedFileLog
        : ILogger
    {
        #region Fields
        private readonly EncryptedFileLogOptions _options;
        private int _todayCountLogFiles = 0;
        /// <summary>
        /// Current log file
        /// </summary>
        private string _currentLogFile;
        /// <summary>
        /// Stream to output to file
        /// </summary>
        private Stream _writer;
        /// <summary>
        /// Lock on re-create file
        /// </summary>
        private readonly object _fileRecreating = new object();

        private long _currentLogSize = 0;

        private readonly long _taskRename = -1;

        private readonly FastObfuscator _obfuscator;
        #endregion

        public EncryptedFileLog(EncryptedFileLogOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.Key))
                throw new ArgumentNullException("options.Key");
            _options = options;
            if (string.IsNullOrWhiteSpace(_options.Folder))
            {
                _options.SetFolderPath(Path.Combine(Configuration.BaseDirectory, "logs"));
            }
            if (!Directory.Exists(_options.Folder))
            {
                var dir = Directory.CreateDirectory(_options.Folder);
                if (dir.Exists == false)
                {
                    throw new ArgumentException($"Can't create or found directory '{_options.Folder}'");
                }
            }
            _obfuscator = new FastObfuscator(options.Key);
            RecreateLogFile();
            _taskRename = Sheduller.RemindEveryNonlinearPeriod(() =>
                (DateTime.Now.AddDays(1).Date - DateTime.Now).Add(TimeSpan.FromMilliseconds(100)),
                RecreateLogFile);
        }

        #region Utils
        /// <summary>
        /// Checking the name of the log file (changes when the date changes to the next day)
        /// </summary>
        private void RecreateLogFile()
        {
            lock (_fileRecreating)
            {
                var nextFileName = GetNextFileName();
                CloseCurrentWriter();
                Stream stream = null;
                try
                {
                    _currentLogFile = nextFileName;
                    _writer = new FileStream(_currentLogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
                }
                catch
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                    throw;
                }
            }
        }
        /// <summary>
        /// Closing the current log
        /// </summary>
        private void CloseCurrentWriter()
        {
            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }
        }

        private string GetNextFileName()
        {
            string fileName = Path.Combine(_options.Folder, string.Format(CultureInfo.CurrentCulture, "{0:yyyyMMdd}_{1:D4}.bin", DateTime.Now, _todayCountLogFiles));
            if (_options.LimitFileSize > 0)
            {
                if (!File.Exists(fileName))
                {
                    _todayCountLogFiles = 0;
                }
                else
                {
                    while (File.Exists(fileName))
                    {
                        var length = (new FileInfo(fileName).Length >> 10);
                        if (length >= _options.LimitFileSize)
                        {
                            _todayCountLogFiles++;
                            fileName = Path.Combine(_options.Folder, string.Format(CultureInfo.CurrentCulture, "{0:yyyyMMdd}_{1:D4}.bin", DateTime.Now, _todayCountLogFiles));
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            return fileName;
        }
        #endregion

        #region IDisposable
        private bool _disposed = false;
        public void Dispose()
        {
            Sheduller.Remove(_taskRename);
            if (false == _disposed)
            {
                _disposed = true;
                CloseCurrentWriter();
            }
        }
        #endregion

        #region ILog
        public void Write(LogLevel level, string message)
        {
            if (false == _disposed)
            {
                byte[] data;
                if (level == LogLevel.Raw)
                {
                    data = Encoding.UTF8.GetBytes(message);
                }
                else
                {
                    data = Encoding.UTF8.GetBytes($"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")} {LogLevelNameMapping.CompactName(level)}]\t{message}\r\n");
                }
                _obfuscator.HashData(data);
                lock (_fileRecreating)
                {
                    _writer.Write(BitConverter.GetBytes(data.Length), 0, 4);
                    _writer.Write(data, 0, data.Length);
                }
                _currentLogSize += data.Length;
                if (_options.LimitFileSize > 0 && _currentLogSize >> 10 >= _options.LimitFileSize)
                {
                    RecreateLogFile();
                }
            }
        }
        #endregion
    }
}
