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
        public static string BaseDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public static string AppLocation = Assembly.GetEntryAssembly()?.Location;

        public const string DEFAULT_SECTION_NAME = "_defaultsection";

        #region Ctor

        static Configuration()
        {
            _empty = new BaseConfiguration();
            _emptySet = new BaseConfigurationSet();
            _empty.Freeze(true);
            _emptySet.FreezeConfiguration(true);
            DefaultSet = Configuration.CreateSet();
        }

        #endregion Ctor

        #region Cachee

        private static readonly IConfiguration _empty;
        private static readonly IConfigurationSet _emptySet;
        private static readonly ConcurrentDictionary<string, IConfiguration> _cachee = new ConcurrentDictionary<string, IConfiguration>();
        private static readonly ConcurrentDictionary<string, IConfigurationSet> _setCachee = new ConcurrentDictionary<string, IConfigurationSet>();

        public static IConfiguration Empty { get { return _empty; } }
        public static IConfigurationSet EmptySet { get { return _emptySet; } }

        public static IConfiguration Default => DefaultSet?.Default;
        public static IConfigurationSet DefaultSet { get; private set; }

        public static void Save(string name, IConfiguration configuration)
        {
            _cachee.AddOrUpdate(name, configuration, (oldKey, oldValue) => configuration);
        }

        public static void Save(IConfiguration configuration)
        {
            if (DefaultSet == null)
            {
                DefaultSet = Configuration.CreateSet(configuration);
            }
            else
            {
                throw new Exception("Default configuration exists already");
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
        /// Creating a configuration from the AppSettings section of the app.config or web.config file
        /// </summary>
        /// <returns>Configuration</returns>
        public static IConfiguration ReadFromApplicationConfig() { return new ApplicationConfigReader().ReadConfiguration(); }

        /// <summary>
        /// Creating a configuration from the AppSettings section of the app.config file or web.config, is supplemented by the 'ConnectionStrings' section
        /// </summary>
        /// <returns>Configuration</returns>
        public static IConfigurationSet ReadSetFromApplicationConfig() { return new ApplicationConfigReader().ReadConfigurationSet(); }

        /// <summary>
        /// Creating a configuration from the AppSettings section of the app.config or web.config file
        /// </summary>
        /// <returns>Configuration</returns>
        public static IConfiguration ReadFromApplicationConfig(string configFilePath) { return new ApplicationConfigReader(configFilePath).ReadConfiguration(); }

        /// <summary>
        /// Creating a configuration from the AppSettings section of the app.config file or web.config, is supplemented by the 'ConnectionStrings' section
        /// </summary>
        /// <returns>Configuration</returns>
        public static IConfigurationSet ReadSetFromApplicationConfig(string configFilePath) { return new ApplicationConfigReader(configFilePath).ReadConfigurationSet(); }

        /// <summary>
        /// Create configuration from ini file
        /// </summary>
        /// <param name="path">Path to the ini file</param>
        /// <returns>Configuration</returns>
        public static IConfiguration ReadFromIniFile(string path) { return new IniFileReader(path).ReadConfiguration(); }

        /// <summary>
        /// Creating a configuration from an ini file, including sections
        /// </summary>
        /// <param name="path">Path to the ini file</param>
        /// <returns>Configuration</returns>
        public static IConfigurationSet ReadSetFromIniFile(string path) { return new IniFileReader(path).ReadConfigurationSet(); }

        /// <summary>
        /// Creating configuration from command line parameters
        /// </summary>
        /// <param name="args">Command line parameters</param>
        /// <returns>Configuration</returns>
        public static IConfiguration ReadFromCommandLine(string[] args) { return new CommandLineReader(args).ReadConfiguration(); }

        public static IConfigurationSet ReadBinary(IBinaryReader reader)
        {
            return reader.Read<BaseConfigurationSet>();
        }

        #endregion Read configuration

        #region Write configuration

        /// <summary>
        /// Write a simple configuration to the ini file
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="path">Path to the ini file</param>
        public static void WriteToIniFile(IConfiguration configuration, string path) { new IniFileWriter(path).WriteConfiguration(configuration); }

        /// <summary>
        /// Write the complete configuration to the ini-file
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="path">Path to the ini file</param>
        public static void WriteSetToIniFile(IConfigurationSet configuration, string path) { new IniFileWriter(path).WriteConfigurationSet(configuration); }

        #endregion Write configuration
    }
}