using System;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.IO;
using ZeroLevel.Services.Formats.YAML;

namespace ZeroLevel.Services.Config.Implementation
{
    internal sealed class YamlFileReader
        : IConfigurationReader
    {
        private readonly string _yamlPath;

        internal YamlFileReader(string configPath)
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
            _yamlPath = configPath;
        }

        public IConfiguration ReadConfiguration()
        {
            var set = ReadConfigurationSet();
            return set.Default;
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public IConfigurationSet ReadConfigurationSet()
        {
            try
            {
                var yaml = File.ReadAllText(_yamlPath);
                var json = FullYamlToJsonConverter.Convert(yaml);

                using (Stream stream = GenerateStreamFromString(json))
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
                Log.Error(ex, $"[YamlFileReader] Failed to load configuration from file '{_yamlPath}'.");
                throw new InvalidDataException($"Failed to load configuration from file '{_yamlPath}'.");
            }
        }



        private static class FullYamlToJsonConverter
        {
            public static string Convert(string yaml)
            {
                var deserializer = new DeserializerBuilder().Build();
                var yamlObject = deserializer.Deserialize(yaml);
                var serializer = new SerializerBuilder()
                    .JsonCompatible()
                    .Build();
                var json = serializer.Serialize(yamlObject);
                return json;
            }
        }
    }
}
