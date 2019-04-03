namespace ZeroLevel.Services.FileSystem
{
    public sealed class FileMeta
    {
        public FileMeta(string name, string path)
        {
            FileName = name;
            FilePath = path;
        }

        public string FileName { get; private set; }
        public string FilePath { get; private set; }
    }
}