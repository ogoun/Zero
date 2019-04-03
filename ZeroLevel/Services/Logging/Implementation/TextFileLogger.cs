using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using ZeroLevel.Services.FileSystem;

namespace ZeroLevel.Services.Logging.Implementation
{
    public sealed class TextFileLoggerOptions
    {
        public TextFileLoggerOptions()
        {
            Folder = null;
            LimitFileSize = 0;
            TextEncoding = DEFAULT_ENCODING;
            RemoveOlderThen = TimeSpan.MinValue;
            RemoveOldFiles = false;
            ZipOldFiles = false;
        }

        public static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;
        internal string Folder { get; private set; }
        internal long LimitFileSize { get; private set; }
        internal Encoding TextEncoding { get; private set; }

        /// <summary>
        /// Delete files older than (aging period)
        /// </summary>
        internal TimeSpan RemoveOlderThen { get; private set; }

        /// <summary>
        /// Delete outdate files
        /// </summary>
        internal bool RemoveOldFiles { get; private set; }

        /// <summary>
        /// Archive files
        /// </summary>
        internal bool ZipOldFiles { get; private set; }

        internal TextFileLoggerOptions Commit()
        {
            if (string.IsNullOrWhiteSpace(Folder))
            {
                throw new ArgumentException("Not set log folder path");
            }
            return this;
        }

        /// <summary>
        /// Enable automatic archiving
        /// </summary>
        public TextFileLoggerOptions EnableAutoArchiving()
        {
            this.ZipOldFiles = true;
            return this;
        }

        /// <summary>
        ///Enable automatic deletion of outdate files.
        /// </summary>
        /// <param name="age">The age of the log file at which removal is required</param>
        public TextFileLoggerOptions EnableAutoCleaning(TimeSpan age)
        {
            this.RemoveOldFiles = true;
            this.RemoveOlderThen = age;
            return this;
        }

        public TextFileLoggerOptions SetFolderPath(string folder)
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

        public TextFileLoggerOptions SetMaximumFileSizeInKb(long size)
        {
            this.LimitFileSize = size;
            return this;
        }

        public TextFileLoggerOptions SetEncoding(Encoding encoding)
        {
            this.TextEncoding = encoding;
            return this;
        }

        internal static TextFileLoggerOptions CreateOptionsBy(IConfiguration config, string logPrefix)
        {
            if (config.Contains(logPrefix))
            {
                var options = new TextFileLoggerOptions().
                    SetFolderPath(config.First(logPrefix));

                config.DoWithFirst<long>($"{logPrefix}.backlog", backlog =>
                {
                    if (backlog > 0)
                    {
                        Log.Backlog(backlog);
                    }
                });
                config.DoWithFirst<bool>($"{logPrefix}.archive", enable =>
                {
                    if (enable)
                    {
                        options.EnableAutoArchiving();
                    }
                });
                config.DoWithFirst<int>($"{logPrefix}.sizeinkb", size =>
                {
                    if (size >= 1)
                    {
                        options.SetMaximumFileSizeInKb(size);
                    }
                });

                config.DoWithFirst<int>($"{logPrefix}.cleanolderdays", days =>
                {
                    if (days > 0)
                    {
                        options.EnableAutoCleaning(TimeSpan.FromDays(days));
                    }
                });
                return options;
            }
            return null;
        }
    }

    public sealed class TextFileLogger : ILogger
    {
        #region Fields

        private readonly TextFileLoggerOptions _options;

        private int _todayCountLogFiles = 0;

        /// <summary>
        /// Current log file
        /// </summary>
        private string _currentLogFile;

        /// <summary>
        /// Stream to output to file
        /// </summary>
        private TextWriter _writer;

        /// <summary>
        /// Lock on re-create file
        /// </summary>
        private readonly object _fileRecreating = new object();

        private readonly HashSet<long> _taskKeys = new HashSet<long>();

        public static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;

        #endregion Fields

        #region Ctors

        /// <summary>
        /// Constructor indicating the directory for recording log files, the encoding is set to Unicode by default.
        /// </summary>
        public TextFileLogger(TextFileLoggerOptions options)
        {
            _options = options.Commit();
            if (!Directory.Exists(_options.Folder))
            {
                var dir = Directory.CreateDirectory(_options.Folder);
                if (dir.Exists == false)
                {
                    throw new ArgumentException($"Can't create or found directory '{_options.Folder}'");
                }
            }
            RecreateLogFile();
            // Maintenance tasks
            if (_options.LimitFileSize > 0)
            {
                _taskKeys.Add(Sheduller.RemindEvery(TimeSpan.FromSeconds(20), CheckRecreateFileLogByOversize));
            }
            if (_options.RemoveOldFiles)
            {
                _taskKeys.Add(Sheduller.RemindEvery(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(60), RemoveOldFiles));
            }
            _taskKeys.Add(Sheduller.RemindEveryNonlinearPeriod(() =>
                (DateTime.Now.AddDays(1).Date - DateTime.Now).Add(TimeSpan.FromMilliseconds(100)),
                RecreateLogFile));
        }

        #endregion Ctors

        #region Private member

        private void RemoveOldFiles()
        {
            try
            {
                var dir = new DirectoryInfo(_options.Folder);
                dir.
                    GetFiles().
                    Do(files =>
                    {
                        foreach (var file in files.Where(f => (DateTime.Now - f.CreationTime) > _options.RemoveOlderThen).ToArray())
                        {
                            file.Delete();
                        }
                    });
                dir = null;
            }
            catch { }
        }

        /// <summary>
        /// Closing the current log
        /// </summary>
        private void CloseCurrentWriter()
        {
            if (_writer != null)
            {
                _writer.Flush();
                _writer.Close();
                _writer.Dispose();
                _writer = null;
            }
        }

        private string GetNextFileName()
        {
            string fileName = Path.Combine(_options.Folder, string.Format(CultureInfo.CurrentCulture, "{0:yyyyMMdd}_{1:D4}.txt", DateTime.Now, _todayCountLogFiles));
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
                            fileName = Path.Combine(_options.Folder, string.Format(CultureInfo.CurrentCulture, "{0:yyyyMMdd}_{1:D4}.txt", DateTime.Now, _todayCountLogFiles));
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

        private void CheckRecreateFileLogByOversize()
        {
            var fi = new FileInfo(_currentLogFile);
            if (fi.Exists && (fi.Length >> 10) >= _options.LimitFileSize)
            {
                RecreateLogFile();
            }
            fi = null;
        }

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
                PackOldLogFile(_currentLogFile);
                try
                {
                    _currentLogFile = nextFileName;
                    stream = new FileStream(_currentLogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
                    _writer = new StreamWriter(stream, _options.TextEncoding);
                    stream = null;
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

        private void PackOldLogFile(string filePath)
        {
            if (null != filePath && File.Exists(filePath) && _options.ZipOldFiles)
            {
                using (var stream = new FileStream($"{filePath}.zip", FileMode.Create))
                {
                    using (var zipStream = new GZipStream(stream, CompressionLevel.Optimal, false))
                    {
                        var buffer = new byte[1024 * 1024];
                        int count = 0;
                        using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None)))
                        {
                            while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                zipStream.Write(buffer, 0, count);
                            }
                            reader.Close();
                        }
                        buffer = null;
                    }
                    stream.Close();
                }
                File.Delete(filePath);
            }
        }

        #endregion Private member

        #region ILog

        public void Write(LogLevel level, string message)
        {
            if (false == _disposed)
            {
                lock (_fileRecreating)
                {
                    if (level == LogLevel.Raw)
                    {
                        _writer.WriteLine(message);
                    }
                    else
                    {
                        var meta = string.Format("[{0:dd'.'MM'.'yyyy HH':'mm':'ss} {1}]\t", DateTime.Now, LogLevelNameMapping.CompactName(level));
                        _writer.Write(meta);
                        _writer.WriteLine(message);
                    }
                    _writer.Flush();
                }
            }
        }

        #endregion ILog

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            foreach (var tk in _taskKeys)
            {
                Sheduller.Remove(tk);
            }
            if (false == _disposed)
            {
                _disposed = true;
                CloseCurrentWriter();
            }
        }

        #endregion IDisposable
    }

    public sealed class FileLogger : ILogger
    {
        #region Fields

        /// <summary>
        /// Stream to output to file
        /// </summary>
        private TextWriter _writer;

        public static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;

        #endregion Fields

        #region Ctors

        public FileLogger(string path)
        {
            CreateLogFile(PreparePath(path));
        }

        #endregion Ctors

        #region Private member

        private static void PrepareFolder(string path)
        {
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
                FSUtils.SetupFolderPermission(path,
                    $"{Environment.UserDomainName}\\{Environment.UserName}",
                    FileSystemRights.Write | FileSystemRights.Read | FileSystemRights.Delete | FileSystemRights.Modify,
                    AccessControlType.Allow);
            }
        }

        private static string PreparePath(string path)
        {
            if (path.IndexOf(':') == -1)
            {
                path = Path.Combine(Configuration.BaseDirectory, path);
            }
            path = FSUtils.PathCorrection(path);
            PrepareFolder(Path.GetDirectoryName(path));
            return path;
        }

        /// <summary>
        /// Closing the current log
        /// </summary>
        private void CloseCurrentWriter()
        {
            if (_writer != null)
            {
                _writer.Flush();
                _writer.Close();
                _writer.Dispose();
                _writer = null;
            }
        }

        private void CreateLogFile(string path)
        {
            Stream stream = null;
            try
            {
                stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(stream, DEFAULT_ENCODING);
                stream = null;
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

        #endregion Private member

        #region ILog

        public void Write(LogLevel level, string message)
        {
            if (false == _disposed)
            {
                if (level == LogLevel.Raw)
                {
                    _writer.WriteLine(message);
                }
                else
                {
                    var meta = string.Format("[{0:dd'.'MM'.'yyyy HH':'mm':'ss} {1}]\t", DateTime.Now, LogLevelNameMapping.CompactName(level));
                    _writer.Write(meta);
                    _writer.WriteLine(message);
                }
                _writer.Flush();
            }
        }

        #endregion ILog

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            if (false == _disposed)
            {
                _disposed = true;
                CloseCurrentWriter();
            }
        }

        #endregion IDisposable
    }
}