using System.IO;

namespace ZeroLevel.Services.Config.Implementation
{
    /// <summary>
    /// Write config to ini-file
    /// </summary>
    public class IniFileWriter 
        : IConfigurationWriter
    {
        /// <summary>
        /// Config file path
        /// </summary>
        private readonly string _iniPath;

        public IniFileWriter(string iniPath)
        {
            _iniPath = iniPath;
        }
        /// <summary>
        /// Write config to file
        /// </summary>
        public void WriteConfiguration(IConfiguration configuration)
        {
            using (TextWriter writer = new StreamWriter(_iniPath, false))
            {
                foreach (string key in configuration.Keys)
                {
                    if (configuration.Count(key) > 0)
                    {
                        foreach (string value in configuration[key])
                        {
                            writer.WriteLine(key.Trim() + "=" + value.Trim());
                        }
                    }
                    else
                    {
                        writer.WriteLine(key.Trim());
                    }
                }
                writer.Flush();
            }
        }
        /// <summary>
        /// Write configuration set to file
        /// </summary>
        public void WriteConfigurationSet(IConfigurationSet configuration)
        {
            using (TextWriter writer = new StreamWriter(_iniPath, false))
            {
                foreach (string section in configuration.SectionNames)
                {
                    if (false == section.Equals(Configuration.DEFAULT_SECTION_NAME, System.StringComparison.Ordinal))
                        writer.WriteLine("[" + section + "]");
                    foreach (string key in configuration[section].Keys)
                    {
                        if (configuration[section].Count(key) > 0)
                        {
                            foreach (string value in configuration[section][key])
                            {
                                writer.WriteLine(key + "=" + value);
                            }
                        }
                        else
                        {
                            writer.WriteLine(key);
                        }
                    }
                }
                writer.Flush();
            }
        }
    }
}
