namespace ZeroLevel.Services.Config.Implementation
{
    internal sealed class ApplicationConfigReader
        : IConfigurationReader
    {
        private readonly AppWebConfigReader _reader;

        internal ApplicationConfigReader()
        {
            _reader = new AppWebConfigReader();
        }

        internal ApplicationConfigReader(string configFilePath)
        {
            _reader = new AppWebConfigReader(configFilePath);
        }

        public IConfiguration ReadConfiguration()
        {
            var result = Configuration.Create();
            foreach (var pair in _reader.ReadAppSettings())
            {
                result.Append(pair.Item1, pair.Item2);
            }
            return result;
        }

        public IConfigurationSet ReadConfigurationSet()
        {
            var result = Configuration.CreateSet();
            foreach (var pair in _reader.ReadAppSettings())
            {
                result.Default.Append(pair.Item1, pair.Item2);
            }
            foreach (var section in _reader.GetSections())
            {
                var sectionConfig = result.CreateSection(section);
                foreach (var pair in _reader.ReadSection(section))
                {
                    sectionConfig.Append(pair.Item1, pair.Item2);
                }
            }
            return result;
        }
    }
}