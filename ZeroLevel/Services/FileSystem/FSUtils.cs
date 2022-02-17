using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroLevel.Services.FileSystem
{
    public static class FSUtils
    {
        public static string GetAppLocalTemporaryDirectory()
        {
            var fn = Path.GetRandomFileName();
            var folderName = Path.Combine(Configuration.BaseDirectory, "temp", fn);
            if (false == Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            return folderName;
        }

        public static string GetAppLocalTemporaryFile()
        {
            var fn = Path.GetRandomFileName();
            var folderName = Path.Combine(Configuration.BaseDirectory, "temp");
            if (false == Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            return Path.Combine(folderName, fn);
        }

        public static string GetAppLocalTempDirectory(string folderName)
        {
            folderName = Path.Combine(Configuration.BaseDirectory, "temp", folderName);
            if (false == Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            return folderName;
        }

        public static string GetAppLocalDbDirectory(string dbFolderName = null)
        {
            dbFolderName = Path.Combine(Configuration.BaseDirectory, dbFolderName ?? "db");
            if (false == Directory.Exists(dbFolderName))
            {
                Directory.CreateDirectory(dbFolderName);
            }
            return dbFolderName;
        }

        #region FileName & Path correction

        private static string _invalid_path_characters = new string(Path.GetInvalidPathChars());
        private static string _invalid_filename_characters = new string(Path.GetInvalidFileNameChars());
        private static HashSet<string> _invalidRootFileNames = new HashSet<string> { "$mft", "$mftmirr", "$logfile", "$volume", "$attrdef", "$bitmap", "$boot", "$badclus", "$secure", "$upcase", "$extend", "$quota", "$objid", "$reparse" };
        private static bool StartWithInvalidWindowsPrefix(string name)
        {
            if (name.Length >= 3)
            {
                switch (name[0])
                {
                    case 'a':
                    case 'A':
                        switch (name[1])
                        {
                            case 'u':
                            case 'U':
                                switch (name[2])
                                {
                                    case 'x':
                                    case 'X':
                                        return name.Length == 3 || name[3] == '.';  // AUX.
                                }
                                break;
                        }
                        break;
                    case 'c':
                    case 'C':
                        switch (name[1])
                        {
                            case 'o':
                            case 'O':
                                switch (name[2])
                                {
                                    case 'n':
                                    case 'N':
                                        return name.Length == 3 || name[3] == '.';  // CON.
                                    case 'm':
                                    case 'M':
                                        return name.Length >= 4 && char.IsDigit(name[3]) && (name.Length == 4 || name[4] == '.'); // COM0 - COM9
                                }
                                break;
                        }
                        break;
                    case 'l':
                    case 'L':
                        switch (name[1])
                        {
                            case 'p':
                            case 'P':
                                switch (name[2])
                                {
                                    case 't':
                                    case 'T':
                                        return name.Length >= 4 && char.IsDigit(name[3]) && (name.Length == 4 || name[4] == '.'); // LPT0 - LPT9
                                }
                                break;
                        }
                        break;
                    case 'p':
                    case 'P':
                        switch (name[1])
                        {
                            case 'r':
                            case 'R':
                                switch (name[2])
                                {
                                    case 'n':
                                    case 'N':
                                        return name.Length == 3 || name[3] == '.';  // PRN.
                                }
                                break;
                        }
                        break;
                    case 'n':
                    case 'N':
                        switch (name[1])
                        {
                            case 'u':
                            case 'U':
                                switch (name[2])
                                {
                                    case 'l':
                                    case 'L':
                                        return name.Length == 3 || name[3] == '.';  // NUL.
                                }
                                break;
                        }
                        break;
                }
            }
            return false;
        }

        private static bool IsFilenameReserverForRootPath(string name) => _invalidRootFileNames.Contains(name.ToLowerInvariant());

        /// <summary>
        /// Removes invalid characters from the passed path
        /// </summary>
        public static string PathCorrection(string path)
        {
            if (path == null) return string.Empty;
            var result = new char[path.Length];
            var index = 0;
            foreach (char c in path)
            {
                if (_invalid_path_characters.IndexOf(c) >= 0)
                {
                    continue;
                }
                result[index] = c;
                index++;
            }            
            return new string(result, 0, index);
        }

        /// <summary>
        /// Removes invalid characters from the passed file name
        /// </summary>
        public static string FileNameCorrection(string name, bool isRootPath = false)
        {
            if (name == null) return string.Empty;
            // The reserved filenames
            if (StartWithInvalidWindowsPrefix(name))
            {
                name = $"@{name}";
            }
            // The reserved NTFS filenames for root path
            if (isRootPath && IsFilenameReserverForRootPath(name))
            {
                name = $"@{name}";
            }
            // Invalid symbols
            var result = new char[name.Length];
            var index = 0;
            foreach (char c in name)
            {
                if (_invalid_filename_characters.IndexOf(c) >= 0)
                {
                    continue;
                }
                result[index] = c;
                index++;
            }
            // Filenames cannot end in a space or dot.
            if (result[index - 1] == '.' || char.IsWhiteSpace(result[index - 1]))
                index--;
            return new string(result, 0, index);
        }

        /// <summary>
        /// Replace invalid characters from the passed file name
        /// </summary>
        public static string FileNameCorrection(string name, char replacedSymbol, bool isRootPath = false)
        {
            if (name == null) return string.Empty;
            if (_invalid_filename_characters.IndexOf(replacedSymbol) >= 0)
            {
                throw new ArgumentException($"The sybmol '{replacedSymbol}' is invalid for windows filenames");
            }
            // The reserved filenames
            if (StartWithInvalidWindowsPrefix(name))
            {
                name = $"@{name}";
            }
            // The reserved NTFS filenames for root path
            if (isRootPath && IsFilenameReserverForRootPath(name))
            {
                name = $"@{name}";
            }
            // Invalid symbols
            var result = new char[name.Length];
            var index = 0;
            foreach (char c in name)
            {
                if (_invalid_filename_characters.IndexOf(c) >= 0)
                {
                    result[index] = replacedSymbol;
                }
                else
                {
                    result[index] = c;
                }
                index++;
            }
            // Filenames cannot end in a space or dot.
            if (result[index - 1] == '.' || char.IsWhiteSpace(result[index - 1]))
                index--;
            return new string(result, 0, index);
        }

        #endregion FileName & Path correction

        /// <summary>
        /// Performs a file accessibility check for processing
        /// </summary>
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream fileStream = null;
            try
            {
                fileStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream.Dispose();
                }
                file = null;
            }
            return false;
        }

        public static bool IsFileLocked(string file)
        {
            FileStream fileStream = null;
            try
            {
                fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream.Dispose();
                }
                file = null;
            }
            return false;
        }

        public static void PackFolder(string sourceFolder, string zipPath, Func<FileInfo, bool> selector = null)
        {
            var tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var tmpDir = Directory.CreateDirectory(tmp);
            var files = new DirectoryInfo(sourceFolder)
                .GetFiles("*.*", SearchOption.AllDirectories)
                .AsEnumerable();
            if (selector != null)
            {
                files = files.Where(selector);
            }
            foreach (var file in files)
            {
                var filepath = Path.Combine(tmp, file.FullName.Replace(sourceFolder, string.Empty).TrimStart('\\', '/'));
                var filedir = Path.GetDirectoryName(filepath);
                if (false == Directory.Exists(filedir))
                {
                    Directory.CreateDirectory(filedir);
                }
                file.CopyTo(filepath, true);
            }
            ZipFile.CreateFromDirectory(tmp, zipPath);
            tmpDir.Delete(true);
        }

        public static void UnPackFolder(byte[] data, string targetFolder)
        {
            if (Directory.Exists(targetFolder))
            {
                try
                {
                    FSUtils.RemoveFolder(targetFolder, 3, 3000);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[FSUtils] Fault clean folder '{Path.GetDirectoryName(targetFolder)}'");
                }
            }
            if (Directory.Exists(targetFolder) == false)
            {
                Directory.CreateDirectory(targetFolder);
            }
            var tmpZip = Path.Combine(Configuration.BaseDirectory, "temp", Path.GetRandomFileName());
            var tmp = Directory.CreateDirectory(tmpZip);
            var zipFile = Path.Combine(tmp.FullName, "zip.zip");
            File.WriteAllBytes(zipFile, data);
            ZipFile.ExtractToDirectory(zipFile, targetFolder);
        }

        public static void CopyDir(string sourceFolder, string targetFolder)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourceFolder, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourceFolder, targetFolder));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourceFolder, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourceFolder, targetFolder), true);
        }

        public static String MakeRelativePath(String fromPath, String toPath)
        {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        public static void RemoveFolder(string path, int fault_retrying_count = 5, int fault_timeout_period = 1000)
        {
            bool deleted = false;
            int try_counter = 0;
            do
            {
                try
                {
                    if (Directory.Exists(path) == false)
                    {
                        deleted = true;
                        break;
                    }
                    else
                    {
                        Directory.Delete(path, true);
                        deleted = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[FSUtils.RemoveFolder] Fault remove folder {path}");
                    try_counter++;
                    Thread.Sleep(fault_timeout_period);
                }
            } while (deleted == false && fault_retrying_count < 5);
        }

        public static async Task RemoveFolderAsync(string path, int fault_retrying_count = 5, int fault_timeout_period = 1000)
        {
            bool deleted = false;
            int try_counter = 0;
            do
            {
                try
                {
                    if (Directory.Exists(path) == false)
                    {
                        deleted = true;
                        break;
                    }
                    else
                    {
                        await Task.Factory.StartNew(p => Directory.Delete((string)p, true), path);
                        deleted = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[FSUtils.RemoveFolderAsync] Fault remove folder {path}");
                    try_counter++;
                    await Task.Delay(fault_timeout_period);
                }
            } while (deleted == false && fault_retrying_count < 5);
        }

        public static void CleanAndTestFolder(string path)
        {
            if (Directory.Exists(path))
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(path);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
            else
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void CopyAndReplace(string source_path, string destination_path)
        {
            if (Directory.Exists(source_path))
            {
                if (Directory.Exists(destination_path) == false)
                {
                    Directory.CreateDirectory(destination_path);
                }

                //Now Create all of the directories
                foreach (string dirPath in Directory.GetDirectories(source_path, "*",
                    SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(source_path, destination_path));
                }

                //Copy all the files & Replaces any files with the same name
                foreach (string file_path in Directory.GetFiles(source_path, "*.*",
                    SearchOption.AllDirectories))
                {
                    File.Copy(file_path, file_path.Replace(source_path, destination_path), true);
                }
            }
        }

        public static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        public static string GetAbsolutePath(string path)
        {
            if (Path.IsPathRooted(path) == false)
            {
                path = Path.Combine(Configuration.BaseDirectory, path);
            }
            return Path.GetFullPath(path);
        }
    }
}