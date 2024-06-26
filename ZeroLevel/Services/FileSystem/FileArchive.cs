﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroLevel.Services.FileSystem
{
    internal abstract class StoreTask
    {
        protected const int DEFAULT_STREAM_BUFFER_SIZE = 16384;

        public void Store()
        {
            try
            {
                StoreImpl();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "Fault store to archive");
            }
        }

        protected static void Transfer(Stream input, Stream output)
        {
            if (input.CanRead == false)
            {
                throw new InvalidOperationException("Input stream can not be read.");
            }
            if (output.CanWrite == false)
            {
                throw new InvalidOperationException("Output stream can not be write.");
            }
            var readed = 0;
            var buffer = new byte[DEFAULT_STREAM_BUFFER_SIZE];
            while ((readed = input.Read(buffer, 0, buffer.Length)) != 0)
            {
                output.Write(buffer, 0, readed);
            }
            output.Flush();
        }

        protected static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        protected abstract void StoreImpl();
    }

    internal sealed class StoreText :
        StoreTask
    {
        private readonly string _text;
        private readonly string _path;

        public StoreText(string text, string path)
        {
            _text = text;
            _path = path;
        }

        protected override void StoreImpl()
        {
            using (var input_stream = GenerateStreamFromString(_text))
            {
                using (var out_stream = File.Create(_path))
                {
                    Transfer(input_stream, out_stream);
                }
            }
        }
    }

    internal sealed class StoreData :
        StoreTask
    {
        private readonly byte[] _data;
        private readonly string _path;

        public StoreData(byte[] data, string path)
        {
            _data = data;
            _path = path;
        }

        protected override void StoreImpl()
        {
            using (var input_stream = new MemoryStream(_data))
            {
                using (var out_stream = File.Create(_path,
                    DEFAULT_STREAM_BUFFER_SIZE,
                    FileOptions.Asynchronous))
                {
                    Transfer(input_stream, out_stream);
                }
            }
        }
    }

    internal sealed class StoreStream :
        StoreTask
    {
        private readonly Stream _stream;
        private readonly string _path;

        public StoreStream(Stream stream, string path)
        {
            _stream = stream;
            _path = path;
        }

        protected override void StoreImpl()
        {
            using (_stream)
            {
                using (var out_stream = File.Create(_path,
                    DEFAULT_STREAM_BUFFER_SIZE,
                    FileOptions.Asynchronous))
                {
                    Transfer(_stream, out_stream);
                }
            }
        }
    }

    internal sealed class StoreFile :
        StoreTask
    {
        private readonly string _input_path;
        private readonly string _path;

        public StoreFile(string input_file_path, string path)
        {
            _input_path = input_file_path;
            _path = path;
        }

        protected override void StoreImpl()
        {
            using (var input_stream = File.Open(_input_path, FileMode.Open,
                FileAccess.Read, FileShare.Read))
            {
                using (var out_stream = File.Create(_path,
                    DEFAULT_STREAM_BUFFER_SIZE,
                    FileOptions.Asynchronous))
                {
                    Transfer(input_stream, out_stream);
                }
            }
        }
    }

    /// <summary>
    /// File archive
    /// </summary>
    public sealed class FileArchive :
        IDisposable
    {
        private static int _counter;

        private static string NextCounter
        { get { return Interlocked.Increment(ref _counter).ToString("X8"); } }

        private readonly string _base_path;
        private const string DIRECTORY_NAME_TEMPLATE = "yyyyMMdd";
        private const string FILE_NAME_TEMPLATE = "HH_mm_ss_{0}{1}";
        private readonly string _extension;
        private readonly bool _override = false;
        private readonly ConcurrentQueue<StoreTask> _tasks = new ConcurrentQueue<StoreTask>();
        private volatile bool _disposed = false;
        private volatile bool _stopped = false;
        private string _currentArchivePath;
        private bool _split_by_date = false;
        private Thread _backThread;

        public FileArchive(string base_path,
            string ext,
            bool split_by_date,
            bool override_if_exists)
        {
            _base_path = PreparePath(base_path);
            _extension = ext;
            _override = override_if_exists;
            _split_by_date = split_by_date;
            RenewArchivePath();
            if (_split_by_date)
            {
                Sheduller.RemindEveryNonlinearPeriod(
                    () => (DateTime.Now.AddDays(1).Date - DateTime.Now).Add(TimeSpan.FromMilliseconds(100)),
                    RenewArchivePath);
            }
            _backThread = new Thread(Consume);
            _backThread.IsBackground = true;
            _backThread.Start();
        }

        private void RenewArchivePath()
        {
            if (_split_by_date)
            {
                var directory_name = DateTime.Now.ToString(DIRECTORY_NAME_TEMPLATE);
                var directory_path = Path.Combine(_base_path, directory_name);
                PrepareFolder(directory_path);
                _currentArchivePath = directory_path;
            }
            else
            {
                var directory_path = _base_path;
                PrepareFolder(directory_path);
                _currentArchivePath = directory_path;
            }
        }

        private void Consume()
        {
            do
            {
                StoreTask result;
                while (_tasks.TryDequeue(out result))
                {
                    result.Store();
                }
                Thread.Sleep(100);
            } while (_disposed == false);
        }

        /// <summary>
        /// Save text to archive
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="subfolder_name">Archive file name (HH_mm_ss_fff_counter.{ext} by default)</param>
        /// <returns></returns>
        public void StoreText(string text, string subfolder_name = null!, string file_name = null!)
        {
            Apply(new StoreText(text, CreateArchiveFilePath(subfolder_name, file_name)));
        }

        /// <summary>
        /// Saving the specified file to the archive
        /// </summary>
        /// <param name="file_path">File path</param>
        /// <param name="subfolder_name">Archive file name (original file name by default)</param>
        /// <returns></returns>
        public void Store(string file_path, string subfolder_name = null!, string file_name = null!)
        {
            Apply(new StoreFile(file_path, CreateArchiveFilePath(subfolder_name, file_name)));
        }

        public void Store(string file_path, bool immediate, string subfolder_name = null!, string file_name = null!)
        {
            if (immediate)
            {
                new StoreFile(file_path, CreateArchiveFilePath(subfolder_name, file_name)).Store();
            }
            else
            {
                Apply(new StoreFile(file_path, CreateArchiveFilePath(subfolder_name, file_name)));
            }
        }

        /// <summary>
        /// Saving data from stream to archive
        /// </summary>
        /// <param name="stream">Data stream for reading</param>
        /// <param name="subfolder_name">Archive file name (HH_mm_ss_fff_counter.{ext} by default)</param>
        /// <returns></returns>
        public void Store(Stream stream, string subfolder_name = null!, string file_name = null!)
        {
            Apply(new StoreStream(stream, CreateArchiveFilePath(subfolder_name, file_name)));
        }

        /// <summary>
        /// Saving data in binary form in the archive
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="subfolder_name">Archive file name (HH_mm_ss_fff_counter.{ext} by default)</param>
        /// <returns></returns>
        public void StoreData(byte[] data, string subfolder_name = null!, string file_name = null!)
        {
            Apply(new StoreData(data, CreateArchiveFilePath(subfolder_name, file_name)));
        }

        private void Apply(StoreTask task)
        {
            if (_stopped == false)
            {
                _tasks.Enqueue(task);
            }
        }

        #region Helpers

        private string CreateArchiveFilePath(string subfolder_name, string filename)
        {
            string archive_file_path;
            if (string.IsNullOrWhiteSpace(subfolder_name))
            {
                if (string.IsNullOrWhiteSpace(filename))
                {
                    do
                    {
                        archive_file_path = Path.Combine(_currentArchivePath,
                            string.Format(DateTime.Now.ToString(FILE_NAME_TEMPLATE),
                                NextCounter, _extension));
                    } while (_override == false && File.Exists(archive_file_path));
                }
                else
                {
                    archive_file_path = Path.Combine(_currentArchivePath, filename);
                }
            }
            else
            {
                var base_path = Path.Combine(_currentArchivePath, subfolder_name);
                PrepareFolder(base_path);
                if (string.IsNullOrWhiteSpace(filename))
                {
                    do
                    {
                        archive_file_path = Path.Combine(base_path,
                            string.Format(DateTime.Now.ToString(FILE_NAME_TEMPLATE),
                                NextCounter, _extension));
                    } while (_override == false && File.Exists(archive_file_path));
                }
                else
                {
                    archive_file_path = Path.Combine(base_path, filename);
                }
            }
            return archive_file_path;
        }

        private static string PreparePath(string path)
        {
            if (path.IndexOf(':') == -1)
            {
                path = Path.Combine(Configuration.BaseDirectory, path);
            }
            return FSUtils.PathCorrection(path);
        }

        #endregion Helpers
        private static void PrepareFolder(string path)
        {
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
            }
        }

        public void Dispose()
        {
            _stopped = true;
            while (_tasks.Count > 0)
            {
                Thread.Sleep(50);
            }
            _disposed = true;
        }
    }

    public sealed class FileBuffer :
        IDisposable
    {
        private readonly string _base_path;
        private readonly BlockingCollection<StoreTask> _tasks = new BlockingCollection<StoreTask>();
        private volatile bool _disposed = false;
        private volatile bool _stopped = false;
        private string _currentArchivePath;
        private Thread _storeThread;

        public FileBuffer(string base_path)
        {
            _base_path = PreparePath(base_path);
            var directory_path = _base_path;
            PrepareFolder(directory_path);
            _currentArchivePath = directory_path;
            _storeThread = new Thread(Consume);
            _storeThread.IsBackground = true;
            _storeThread.Start();
        }

        private void Consume()
        {
            StoreTask result;
            while (_disposed == false)
            {
                result = _tasks.Take();
                try
                {
                    result.Store();
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[FileBuffer] Fault store file");
                }
            }
        }

        /// <summary>
        /// Save text to archive
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="name">Archive file name (HH_mm_ss_fff_counter.{ext} by default)</param>
        /// <returns></returns>
        public void StoreText(string text, string name = null!)
        {
            Apply(new StoreText(text, CreateArchiveFilePath(name)));
        }

        /// <summary>
        /// Saving the specified file to the archive
        /// </summary>
        /// <param name="file_path">File path</param>
        /// <param name="subfolder_name">Archive file name (original file name by default)</param>
        /// <returns></returns>
        public void Store(string file_path, string name = null!)
        {
            Apply(new StoreFile(file_path, CreateArchiveFilePath(name)));
        }

        /// <summary>
        /// Sync saving the specified file to the archive
        /// </summary>
        /// <param name="file_path">File path</param>
        /// <param name="subfolder_name">Archive file name (original file name by default)</param>
        /// <returns></returns>
        public void Store(string file_path, bool immediate, string name = null!)
        {
            if (immediate)
            {
                new StoreFile(file_path, CreateArchiveFilePath(name)).Store();
            }
            else
            {
                Apply(new StoreFile(file_path, CreateArchiveFilePath(name)));
            }
        }

        /// <summary>
        /// Saving data from stream to archive
        /// </summary>
        /// <param name="stream">Data stream for reading</param>
        /// <param name="name">Archive file name (HH_mm_ss_fff_counter.{ext} by default)</param>
        /// <returns></returns>
        public void Store(Stream stream, string name = null!)
        {
            Apply(new StoreStream(stream, CreateArchiveFilePath(name)));
        }

        /// <summary>
        /// Saving data in binary form in the archive
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="name">Archive file name (HH_mm_ss_fff_counter.{ext} by default)</param>
        /// <returns></returns>
        public void StoreData(byte[] data, string name = null!)
        {
            Apply(new StoreData(data, CreateArchiveFilePath(name)));
        }

        private void Apply(StoreTask task)
        {
            if (_stopped == false)
            {
                _tasks.Add(task);
            }
        }

        #region Helpers

        private string CreateArchiveFilePath(string original_name)
        {
            return Path.Combine(_currentArchivePath, FSUtils.FileNameCorrection(original_name));
        }

        private static void PrepareFolder(string path)
        {
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
            }
        }

        private static string PreparePath(string path)
        {
            if (path.IndexOf(':') == -1)
            {
                path = Path.Combine(Configuration.BaseDirectory, path);
            }
            return FSUtils.PathCorrection(path);
        }

        #endregion Helpers

        public void Dispose()
        {
            _stopped = true;
            _tasks.CompleteAdding();
            while (_tasks.Count > 0)
            {
                Thread.Sleep(50);
            }
            _disposed = true;
        }
    }
}