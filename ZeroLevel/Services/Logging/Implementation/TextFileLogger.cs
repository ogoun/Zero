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
        /// Удалять файлы старее чем (период устаревания)
        /// </summary>
        internal TimeSpan RemoveOlderThen { get; private set; }
        /// <summary>
        /// Удалять устаревшие файлы
        /// </summary>
        internal bool RemoveOldFiles { get; private set; }
        /// <summary>
        /// Архивировать файлы
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
        /// Включить автоматическое архивирование
        /// </summary>
        public TextFileLoggerOptions EnableAutoArchiving()
        {
            this.ZipOldFiles = true;
            return this;
        }
        /// <summary>
        /// Включить автоматическое удаление устаревших файлов
        /// </summary>
        /// <param name="age">Возраст файла лога по достижении которого требуется удаление</param>
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

                config.DoWithFirst<long>(string.Format("{0}.backlog", logPrefix), backlog =>
                {
                    if (backlog > 0)
                    {
                        Log.Backlog(backlog);
                    }
                });
                config.DoWithFirst<bool>(string.Format("{0}.archive", logPrefix), enable =>
                {
                    if (enable)
                    {
                        options.EnableAutoArchiving();
                    }
                });
                config.DoWithFirst<int>(string.Format("{0}.sizeinkb", logPrefix), size =>
                {
                    if (size >= 1)
                    {
                        options.SetMaximumFileSizeInKb(size);
                    }
                });

                config.DoWithFirst<int>(string.Format("{0}.cleanolderdays", logPrefix), days =>
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
        /// Текущий лог-файл
        /// </summary>
        private string _currentLogFile;
        /// <summary>
        /// Поток для вывода в файл
        /// </summary>
        private TextWriter _writer;
        /// <summary>
        /// Лок на пересоздание файла
        /// </summary>
        private readonly object _fileRecreating = new object();

        private readonly HashSet<long> _taskKeys = new HashSet<long>();

        public static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;
        #endregion

        #region Ctors

        /// <summary>
        /// Конструктор с указанием каталога для записи лог-файлов, кодировка задается по умолчанию как Unicode
        /// </summary>
        /// <param name="options"></param>
        public TextFileLogger(TextFileLoggerOptions options)
        {
            _options = options.Commit();
            if (!Directory.Exists(_options.Folder))
            {
                var dir = Directory.CreateDirectory(_options.Folder);
                if (dir.Exists == false)
                {
                    throw new ArgumentException(string.Format("Can't create or found directory '{0}'", _options.Folder));
                }
            }
            RecreateLogFile();
            // Задачи обслуживания
            // Пересоздание лог-файла при достижении размера больше указанного
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
        #endregion

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
        /// Закрытие текущего лога
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
        /// Проверка имени лог-файла (изменяется при смене даты на следующий день)
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
                using (var stream = new FileStream(string.Format("{0}.{1}", filePath, "zip"), FileMode.Create))
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
        #endregion

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
        #endregion

        #region IDisposable
        private bool _disposed = false;
        /// <summary>
        /// Освобождение рессурсов
        /// </summary>
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
        #endregion
    }

    public sealed class FileLogger : ILogger
    {
        #region Fields
        /// <summary>
        /// Поток для вывода в файл
        /// </summary>
        private TextWriter _writer;
        public static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;
        #endregion

        #region Ctors

        /// <summary>
        /// Конструктор с указанием каталога для записи лог-файлов, кодировка задается по умолчанию как Unicode
        /// </summary>
        /// <param name="options"></param>
        public FileLogger(string path)
        {
            CreateLogFile(PreparePath(path));
        }
        #endregion

        #region Private member
        private static void PrepareFolder(string path)
        {
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
                FSUtils.SetupFolderPermission(path,
                    string.Format("{0}\\{1}", Environment.UserDomainName, Environment.UserName),
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
        /// Закрытие текущего лога
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
        #endregion

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
        #endregion

        #region IDisposable
        private bool _disposed = false;
        /// <summary>
        /// Освобождение рессурсов
        /// </summary>
        public void Dispose()
        {
            if (false == _disposed)
            {
                _disposed = true;
                CloseCurrentWriter();
            }
        }
        #endregion
    }
}
