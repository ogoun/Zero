using System;
using System.Globalization;
using System.IO;

namespace ZeroLevel.Services.FileSystem
{
    public sealed class PeriodicFileSystemWatcher :
        IDisposable
    {
        private long _updateSourceTaskKey;
        private readonly string _sourceFolder;
        private readonly string _temporaryFolder;
        private readonly TimeSpan _period;
        private readonly Action<FileMeta> _callback;

        public event Action<int> OnStartMovingFilesToTemporary = delegate { };
        public event Action OnMovingFileToTemporary = delegate { };
        public event Action OnCompleteMovingFilesToTemporary = delegate { };

        private readonly bool _autoRemoveTempFileAfterCallback = false;

        public PeriodicFileSystemWatcher(TimeSpan period, string watch_folder, string temp_folder, Action<FileMeta> callback, bool removeTempFileAfterCallback = false)
        {            
            if (string.IsNullOrWhiteSpace(watch_folder))
            {
                throw new ArgumentNullException(nameof(watch_folder));
            }
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            _autoRemoveTempFileAfterCallback = removeTempFileAfterCallback;
            _callback = callback;
            _sourceFolder = watch_folder;
            _temporaryFolder = temp_folder;
            _period = period;
            if (_temporaryFolder.IndexOf(':') < 0)
            {
                _temporaryFolder = Path.Combine(Configuration.BaseDirectory, _temporaryFolder);
            }
            if (false == Directory.Exists(_temporaryFolder))
            {
                Directory.CreateDirectory(_temporaryFolder);
            }
        }

        public void Start()
        {
            _updateSourceTaskKey = Sheduller.RemindEvery(_period, CheckSourceFolder);
        }

        public void Stop()
        {
            Sheduller.Remove(_updateSourceTaskKey);
        }

        private void CheckSourceFolder()
        {
            try
            {
                var files = GetFilesFromSource();
                OnStartMovingFilesToTemporary?.Invoke(files.Length);
                foreach (var file in files)
                {
                    try
                    {
                        Log.Debug($"[PeriodicFileSystemWatcher.CheckSourceFolder] Find new file {file}");
                        if (FSUtils.IsFileLocked(new FileInfo(file)))
                        {
                            continue;
                        }
                        string tempFile;
                        try
                        {
                            tempFile = MoveToTemporary(file);
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[PeriodicFileSystemWatcher.CheckSourceFolder] Failed to attempt to move file '{file}' to temporary directory '{_temporaryFolder}'");
                            continue;
                        }
                        finally
                        {
                            OnMovingFileToTemporary?.Invoke();
                        }
                        Log.Debug($"[PeriodicFileSystemWatcher.CheckSourceFolder] Handle file {file}");
                        try
                        {
                            _callback(new FileMeta(Path.GetFileName(file), tempFile));
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[PeriodicFileSystemWatcher.CheckSourceFolder] Fault callback for file '{tempFile}'");
                        }
                        if (_autoRemoveTempFileAfterCallback)
                        {
                            File.Delete(tempFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[PeriodicFileSystemWatcher.CheckSourceFolder] Fault proceed file {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[PeriodicFileSystemWatcher.CheckSourceFolder] Failed to process input directory '{_sourceFolder}'");
            }
            finally
            {
                OnCompleteMovingFilesToTemporary?.Invoke();
            }
        }

        /// <summary>
        /// Moving a file to a temporary directory
        /// </summary>
        private string MoveToTemporary(string from)
        {
            if (from == null)
            {
                throw new ArgumentException("from");
            }
            string tempFile = Path.Combine(_temporaryFolder, Path.GetFileName(from));
            if (File.Exists(tempFile))
            {
                tempFile = TrySolveCollision(tempFile);
            }
            File.Copy(from, tempFile, false);
            File.Delete(from);
            return tempFile;
        }

        /// <summary>
        /// Resolving collisions in filenames in the temporary directory
        /// </summary>
        private static string TrySolveCollision(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            string extension = Path.GetExtension(file);
            string directoryName = Path.GetDirectoryName(file);
            if (directoryName != null)
            {
                int num = 0;
                do
                {
                    num++;
                }
                while (File.Exists(Path.Combine(directoryName,
                fileNameWithoutExtension + "_" +
                num.ToString(CultureInfo.CurrentCulture) +
                extension)));
                return Path.Combine(directoryName,
                    fileNameWithoutExtension + "_" +
                    num.ToString(CultureInfo.CurrentCulture) +
                    extension);
            }
            throw new ArgumentException("folder");
        }

        /// <summary>
        /// Getting a list of files from the input directory
        /// </summary>
        private string[] GetFilesFromSource()
        {
            string[] files = Directory.GetFiles(_sourceFolder, "*.*", SearchOption.TopDirectoryOnly);
            Array.Sort<string>(files, FileNameSortCompare);
            return files;
        }

        /// <summary>
        /// File Name Comparison
        /// </summary>
        private static int FileNameSortCompare(string x, string y)
        {
            int num;
            int num2;
            if (int.TryParse(Path.GetFileNameWithoutExtension(x), out num) &&
                int.TryParse(Path.GetFileNameWithoutExtension(y), out num2))
            {
                return num - num2;
            }
            return string.Compare(x, y, StringComparison.InvariantCultureIgnoreCase);
        }

        public void Dispose()
        {
            Sheduller.Remove(_updateSourceTaskKey);
        }
    }
}