using System;
using System.IO;

namespace ZeroLevel.Services.Config.Implementation
{
    /// <summary>
    /// Read from JSON file, aka 'ConfigurationBuilder().AddJsonFile(configPath)' from Microsoft.Extensions.Configuration
    /// </summary>
    internal sealed class JsonFileReader
        : IConfigurationReader
    {
        private readonly string _jsonPath;

        internal JsonFileReader(string configPath)
        {
            if (String.IsNullOrWhiteSpace(configPath))
            {
                Log.Fatal($"[{nameof(JsonFileReader)}] File path is null or empty");
                throw new ArgumentNullException("configPath", "File path is null or empty");
            }
            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(Configuration.BaseDirectory, configPath);
                if (!File.Exists(configPath))
                {
                    Log.Fatal($"[{nameof(JsonFileReader)}] File path '{configPath}' not exists");
                    throw new FileNotFoundException($"File path '{configPath}' not exists");
                }
            }
            _jsonPath = configPath;
        }

        public IConfiguration ReadConfiguration()
        {
            var set = ReadConfigurationSet();
            return set.Default;
        }

        public IConfigurationSet ReadConfigurationSet()
        {
            try
            {
                using (Stream stream = new FileStream(_jsonPath,
                                FileMode.Open,
                                FileAccess.Read,
                                FileShare.ReadWrite,
                                bufferSize: 1,
                                FileOptions.SequentialScan))
                {
                    IConfigurationSet set = Configuration.CreateSet();
                    var dict = JsonConfigurationFileParser.Parse(stream);
                    foreach (var kv in dict)
                    {
                        if (string.CompareOrdinal(Configuration.DEFAULT_SECTION_NAME, kv.Key) == 0)
                        {
                            foreach (var set_kv in kv.Value)
                            {
                                set.Default.Append(set_kv.Key, set_kv.Value);
                            }
                        }
                        else
                        {
                            var sectionName = kv.Key;
                            IConfiguration section;
                            if (false == set.ContainsSection(sectionName))
                            {
                                section = set.CreateSection(sectionName);
                            }
                            else
                            {
                                section = set.GetSection(sectionName);
                            }
                            foreach (var set_kv in kv.Value)
                            {
                                section.Append(set_kv.Key, set_kv.Value);
                            }
                        }
                    }
                    return set;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[JsonFileReader] Failed to load configuration from file '{_jsonPath}'.");
                throw new InvalidDataException($"Failed to load configuration from file '{_jsonPath}'.");
            }
        }
    }
}
