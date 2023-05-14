using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using ZeroLevel.Services.Config;
using ZeroLevel.Services.Config.Implementation;
using ZeroLevel.Services.Reflection;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel
{
    public static class Configuration
    {
        /// <summary>
        /// Application folder path
        /// </summary>
        public static readonly string BaseDirectory;

        public static readonly string AppLocation;

        public const string DEFAULT_SECTION_NAME = "_defaultsection";

        #region Ctor
        static Configuration()
        {
            _empty = new BaseConfiguration();
            _emptySet = new BaseConfigurationSet();
            _empty.Freeze(true);
            _emptySet.FreezeConfiguration(true);
            DefaultSet = Configuration.CreateSet();
            var assembly = EntryAssemblyAttribute.GetEntryAssembly();
            if (assembly != null)
            {
                BaseDirectory = Path.GetDirectoryName(assembly.Location);
                AppLocation = assembly.Location;
            }
            else
            {
                BaseDirectory = Directory.GetCurrentDirectory();
            }
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
                DefaultSet.Merge(Configuration.CreateSet(configuration));
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
                DefaultSet.Merge(configuration);
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
        public static IConfiguration ReadFromEnvironmentVariables()
        {
            try
            {
                return new EnvironmentVariablesConfigReader().ReadConfiguration();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadFromEnvironmentVariables] Can't read environment variables");
                throw;
            }
        }

        /// <summary>
        /// Creating a configuration from the AppSettings section of the app.config or web.config file
        /// </summary>
        /// <returns>Configuration</returns>
        public static IConfiguration ReadFromApplicationConfig()
        {
            try
            {
                return new ApplicationConfigReader().ReadConfiguration();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadFromApplicationConfig] Can't read app.config file");
                throw;
            }
        }
        public static IConfiguration ReadOrEmptyFromApplicationConfig()
        {
            try
            {
                return new ApplicationConfigReader().ReadConfiguration();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadOrEmptyFromApplicationConfig] Can't read app.config file");
            }
            return _empty;
        }

        /// <summary>
        /// Creating a configuration from the AppSettings section of the app.config file or web.config, is supplemented by the 'ConnectionStrings' section
        /// </summary>
        /// <returns>Configuration</returns>
        public static IConfigurationSet ReadSetFromApplicationConfig()
        {
            try
            {
                return new ApplicationConfigReader().ReadConfigurationSet();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadSetFromApplicationConfig] Can't read app.config file");
                throw;
            }
        }
        public static IConfigurationSet ReadOrEmptySetFromApplicationConfig()
        {
            try
            {
                return new ApplicationConfigReader().ReadConfigurationSet();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadOrEmptySetFromApplicationConfig] Can't read app.config file");
            }
            return _emptySet;
        }

        /// <summary>
        /// Creating a configuration from the AppSettings section of the app.config or web.config file
        /// </summary>
        /// <returns>Configuration</returns>
        public static IConfiguration ReadFromApplicationConfig(string configFilePath)
        {
            try
            {
                return new ApplicationConfigReader(configFilePath).ReadConfiguration();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadFromApplicationConfig] Can't read config file '{configFilePath}'");
                throw;
            }
        }
        public static IConfiguration ReadOrEmptyFromApplicationConfig(string configFilePath)
        {
            try
            {
                return new ApplicationConfigReader(configFilePath).ReadConfiguration();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadOrEmptyFromApplicationConfig] Can't read config file '{configFilePath}'");
            }
            return _empty;
        }

        /// <summary>
        /// Creating a configuration from the AppSettings section of the app.config file or web.config, is supplemented by the 'ConnectionStrings' section
        /// </summary>
        /// <returns>Configuration</returns>
        public static IConfigurationSet ReadSetFromApplicationConfig(string configFilePath)
        {
            try
            {
                return new ApplicationConfigReader(configFilePath).ReadConfigurationSet();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadSetFromApplicationConfig] Can't read config file '{configFilePath}'");
                throw;
            }
        }
        public static IConfigurationSet ReadOrEmptySetFromApplicationConfig(string configFilePath)
        {
            try
            {
                return new ApplicationConfigReader(configFilePath).ReadConfigurationSet();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadOrEmptySetFromApplicationConfig] Can't read config file '{configFilePath}'");
            }
            return _emptySet;
        }

        /// <summary>
        /// Create configuration from ini file
        /// </summary>
        /// <param name="path">Path to the ini file</param>
        /// <returns>Configuration</returns>
        public static IConfiguration ReadFromIniFile(string path)
        {
            try
            {
                return new IniFileReader(path).ReadConfiguration();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadFromIniFile] Can't read config file '{path}'");
                throw;
            }
        }
        public static IConfiguration ReadOrEmptyFromIniFile(string path)
        {
            try
            {
                return new IniFileReader(path).ReadConfiguration();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadOrEmptyFromIniFile] Can't read config file '{path}'");
            }
            return _empty;
        }

        /// <summary>
        /// Creating a configuration from an ini file, including sections
        /// </summary>
        /// <param name="path">Path to the ini file</param>
        /// <returns>Configuration</returns>
        public static IConfigurationSet ReadSetFromIniFile(string path)
        {
            try
            {
                return new IniFileReader(path).ReadConfigurationSet();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadSetFromIniFile] Can't read config file '{path}'");
                throw;
            }
        }
        public static IConfigurationSet ReadOrEmptySetFromIniFile(string path)
        {
            try
            {
                return new IniFileReader(path).ReadConfigurationSet();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadOrEmptySetFromIniFile] Can't read config file '{path}'");
            }
            return _emptySet;
        }

        /// <summary>
        /// Creating configuration from command line parameters
        /// </summary>
        /// <param name="args">Command line parameters</param>
        /// <returns>Configuration</returns>
        public static IConfiguration ReadFromCommandLine(string[] args)
        {
            try
            {
                return new CommandLineReader(args).ReadConfiguration();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadFromCommandLine] Can't read command line args");
                throw;
            }
        }
        public static IConfiguration ReadOrEmptyFromCommandLine(string[] args)
        {
            try
            {
                return new CommandLineReader(args).ReadConfiguration();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadOrEmptyFromCommandLine] Can't read command line args");
            }
            return _empty;
        }

        public static IConfiguration ReadFromBinaryReader(IBinaryReader reader)
        {
            try
            {
                return reader.Read<BaseConfiguration>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadFromBinaryReader] Can't read config from binaryReader");
                throw;
            }
        }
        public static IConfiguration ReadOrEmptyFromBinaryReader(IBinaryReader reader)
        {
            try
            {
                return reader.Read<BaseConfiguration>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadOrEmptyFromBinaryReader] Can't read config from binaryReader");
            }
            return _empty;
        }

        public static IConfigurationSet ReadSetFromBinaryReader(IBinaryReader reader)
        {
            try
            {
                return reader.Read<BaseConfigurationSet>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadSetFromBinaryReader] Can't read config from binaryReader");
                throw;
            }
        }
        public static IConfigurationSet ReadSetOrEmptyFromBinaryReader(IBinaryReader reader)
        {
            try
            {
                return reader.Read<BaseConfigurationSet>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[Configuration.ReadSetOrEmptyFromBinaryReader] Can't read config from binaryReader");
            }
            return _emptySet;
        }
        #endregion Read configuration

        public static IConfiguration Merge(ConfigurationRecordExistBehavior existRecordBehavior, params IConfiguration[] configurations)
        {
            var result = Configuration.Create();
            foreach (var configuration in configurations)
            {
                result.MergeFrom(configuration, existRecordBehavior);
            }
            return result;
        }

        public static IConfigurationSet Merge(ConfigurationRecordExistBehavior existRecordBehavior, params IConfigurationSet[] configurationSets)
        {
            var result = Configuration.CreateSet();
            foreach (var set in configurationSets)
            {
                foreach (var sectionName in set.SectionNames)
                {
                    var section = result.GetOrCreateSection(sectionName);
                    section.MergeFrom(set[sectionName], existRecordBehavior);
                }
            }
            return result;
        }

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