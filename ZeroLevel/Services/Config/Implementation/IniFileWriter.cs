using System.IO;

namespace ZeroLevel.Services.Config.Implementation
{
    /// <summary>
    /// Запись конфигурации в ini-файл
    /// </summary>
    public class IniFileWriter 
        : IConfigurationWriter
    {
        /// <summary>
        /// Путь к ini-файлу
        /// </summary>
        private readonly string _iniPath;
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="IniFileWriter"/>
        /// </summary>
        /// <param name="iniPath">Путь к ini-файлу</param>
        public IniFileWriter(string iniPath)
        {
            _iniPath = iniPath;
        }
        /// <summary>
        /// Запись простой конфигурации
        /// </summary>
        /// <param name="configuration">Конфигурация</param>
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
        /// Запись конфигурации разбитой по секциям
        /// </summary>
        /// <param name="configuration">Конфигурация</param>
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
