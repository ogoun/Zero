using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ZeroLevel.Services.Config;
using ZeroLevel.Services.Config.Implementation;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel
{
    public static class Configuration
    {
        /// <summary>
        /// Application folder path
        /// </summary>
        public static string BaseDirectory =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.CodeBase)?.
            Replace("file:\\", string.Empty);

        public static string AppLocation = Assembly.GetEntryAssembly()?.Location;

        public const string DEFAULT_SECTION_NAME = "_defaultsection";

        #region Ctor

        static Configuration()
        {
            _empty = new BaseConfiguration();
            _emptySet = new BaseConfigurationSet();
            _empty.Freeze(true);
            _emptySet.FreezeConfiguration(true);
        }

        #endregion Ctor

        #region Cachee

        private static readonly IConfiguration _empty;
        private static readonly IConfigurationSet _emptySet;
        private static readonly ConcurrentDictionary<string, IConfiguration> _cachee = new ConcurrentDictionary<string, IConfiguration>();
        private static readonly ConcurrentDictionary<string, IConfigurationSet> _setCachee = new ConcurrentDictionary<string, IConfigurationSet>();

        public static IConfiguration Empty { get { return _empty; } }
        public static IConfigurationSet EmptySet { get { return _emptySet; } }

        public static IConfiguration Default { get; private set; }
        public static IConfigurationSet DefaultSet { get; private set; }

        public static void Save(string name, IConfiguration configuration)
        {
            _cachee.AddOrUpdate(name, configuration, (oldKey, oldValue) => configuration);
        }

        public static void Save(IConfiguration configuration)
        {
            if (Default == null)
            {
                Default = configuration;
            }
            else
            {
                throw new Exception("Default configuration set already");
            }
        }

        public static void Save(string name, IConfigurationSet configurationSet)
        {
            _setCachee.AddOrUpdate(name, configurationSet, (oldKey, oldValue) => configurationSet);
        }

        public static void Save(IConfigurationSet configuration)
        {
            if (DefaultSet == null)
            {
                DefaultSet = configuration;
            }
            else
            {
                throw new Exception("Default configurationset set already");
            }
        }

        public static IConfiguration Get(string name)
        {
            IConfiguration result;
            if (false == _cachee.TryGetValue(name, out result))
            {
                throw new KeyNotFoundException("Not found configuration '{name}'");
            }
            return result;
        }

        public static IConfigurationSet GetSet(string name)
        {
            IConfigurationSet result;
            if (false == _setCachee.TryGetValue(name, out result))
            {
                throw new KeyNotFoundException("Not found configuration set '{name}'");
            }
            return result;
        }

        #endregion Cachee

        #region Factory

        public static IConfiguration Create()
        {
            return new BaseConfiguration();
        }

        public static IConfigurationSet CreateSet()
        {
            return new BaseConfigurationSet();
        }

        public static IConfigurationSet CreateSet(IConfiguration defaultConfiguration)
        {
            return new BaseConfigurationSet(defaultConfiguration);
        }

        #endregion Factory

        #region Read configuration

        /// <summary>
        /// Создание конфигурации из секции AppSettings файла app.config или web.config
        /// </summary>
        /// <returns>Конфигурация</returns>
        public static IConfiguration ReadFromApplicationConfig() { return new ApplicationConfigReader().ReadConfiguration(); }

        /// <summary>
        /// Создание конфигурации из секции AppSettings файла app.config или web.config, дополняется секцией 'ConnectionStrings'
        /// </summary>
        /// <returns>Конфигурация</returns>
        public static IConfigurationSet ReadSetFromApplicationConfig() { return new ApplicationConfigReader().ReadConfigurationSet(); }

        /// <summary>
        /// Создание конфигурации из секции AppSettings файла app.config или web.config
        /// </summary>
        /// <returns>Конфигурация</returns>
        public static IConfiguration ReadFromApplicationConfig(string configFilePath) { return new ApplicationConfigReader(configFilePath).ReadConfiguration(); }

        /// <summary>
        /// Создание конфигурации из секции AppSettings файла app.config или web.config, дополняется секцией 'ConnectionStrings'
        /// </summary>
        /// <returns>Конфигурация</returns>
        public static IConfigurationSet ReadSetFromApplicationConfig(string configFilePath) { return new ApplicationConfigReader(configFilePath).ReadConfigurationSet(); }

        /// <summary>
        /// Создание конфигурации из ini файла
        /// </summary>
        /// <param name="path">Путь к ini-файлу</param>
        /// <returns>Конфигурация</returns>
        public static IConfiguration ReadFromIniFile(string path) { return new IniFileReader(path).ReadConfiguration(); }

        /// <summary>
        /// Создание конфигурации из ini файла, с учетом секций
        /// </summary>
        /// <param name="path">Путь к ini-файлу</param>
        /// <returns>Конфигурация</returns>
        public static IConfigurationSet ReadSetFromIniFile(string path) { return new IniFileReader(path).ReadConfigurationSet(); }

        /// <summary>
        /// Создание конфигурации из параметров командной строки
        /// </summary>
        /// <param name="args">Параметры командной строки</param>
        /// <returns>Конфигурация</returns>
        public static IConfiguration ReadFromCommandLine(string[] args) { return new CommandLineReader(args).ReadConfiguration(); }

        public static IConfigurationSet ReadBinary(IBinaryReader reader)
        {
            return reader.Read<BaseConfigurationSet>();
        }

        #endregion Read configuration

        #region Write configuration

        /// <summary>
        /// Запись простой конфигурации в ini-файл
        /// </summary>
        /// <param name="configuration">Конфигурация</param>
        /// <param name="path">Путь к ini-файлу</param>
        public static void WriteToIniFile(IConfiguration configuration, string path) { new IniFileWriter(path).WriteConfiguration(configuration); }

        /// <summary>
        /// Запись полной конфигурации в ini-файл
        /// </summary>
        /// <param name="configuration">Конфигурация</param>
        /// <param name="path">Путь к ini-файлу</param>
        public static void WriteSetToIniFile(IConfigurationSet configuration, string path) { new IniFileWriter(path).WriteConfigurationSet(configuration); }

        #endregion Write configuration
    }
}