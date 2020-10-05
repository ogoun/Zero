using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

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
        private readonly HashSet<string> _extensions;

        public event Action<int> OnStartMovingFilesToTemporary = delegate { };
        public event Action OnMovingFileToTemporary = delegate { };
        public event Action OnCompleteMovingFilesToTemporary = delegate { };

        private readonly bool _autoRemoveTempFileAfterCallback = false;
        private readonly bool _useSubdirectories = false;

        public PeriodicFileSystemWatcher(TimeSpan period, string watch_folder, string temp_folder, Action<FileMeta> callback
            , IEnumerable<string> extensions = null
            , bool removeTempFileAfterCallback = false
            , bool useSubdirectories = false)
        {
            if (string.IsNullOrWhiteSpace(watch_folder))
            {
                throw new ArgumentNullException(nameof(watch_folder));
            }
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            _extensions = new HashSet<string>(extensions?.Select(e => e.ToLowerInvariant()) ?? Enumerable.Empty<string>());
            _useSubdirectories = useSubdirectories;
            _autoRemoveTempFileAfterCallback = removeTempFileAfterCallback;
            _callback = callback;
            _sourceFolder = watch_folder;
            _temporaryFolder = temp_folder;
            _period = period;
            if (Path.IsPathRooted(_temporaryFolder) == false)
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
            string[] files = null;
            try
            {
                files = GetFilesFromSource();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[PeriodicFileSystemWatcher.CheckSourceFolder] Failed to process input directory '{_sourceFolder}'");
            }
            if (files == null || files.Length == 0)
            {
                return;
            }
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
                        if (string.IsNullOrWhiteSpace(tempFile))
                        {
                            Log.SystemWarning($"[PeriodicFileSystemWatcher.CheckSourceFolder] Failed to move file '{file}' to temporary directory '{_temporaryFolder}'. Without system error!");
                            continue;
                        }
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
            OnCompleteMovingFilesToTemporary?.Invoke();
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
            if (File.Exists(tempFile))
            {
                File.Delete(from);
                return tempFile;
            }
            return null;
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
            string[] files;
            if (_extensions.Count > 0)
            {
                files = Directory.GetFiles(_sourceFolder, "*.*", _useSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    ?.Where(f => _extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    ?.ToArray();
            }
            else
            {
                files = Directory.GetFiles(_sourceFolder, "*.*", _useSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }
            if (files != null)
            {
                Array.Sort<string>(files, FileNameSortCompare);
            }
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