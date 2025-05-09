﻿using System;
using System.Globalization;
using System.IO;

namespace ZeroLevel.Services.Config.Implementation
{
    /// <summary>
    /// Reading configuration from ini file
    /// </summary>
    internal sealed class IniFileReader
        : IConfigurationReader
    {
        private readonly string _iniPath;

        internal IniFileReader(string configPath)
        {
            if (String.IsNullOrWhiteSpace(configPath))
            {
                Log.Fatal($"[{nameof(IniFileReader)}] File path is null or empty");
                throw new ArgumentNullException("configPath", "File path is null or empty");
            }
            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(Configuration.BaseDirectory, configPath);
                if (!File.Exists(configPath))
                {
                    Log.Fatal($"[{nameof(IniFileReader)}] File path '{configPath}' not exists");
                    throw new FileNotFoundException($"File path '{configPath}' not exists");
                }
            }
            _iniPath = configPath;
        }

        public IConfiguration ReadConfiguration()
        {
            var result = Configuration.Create();
            string sectionName = null!;
            foreach (var line in File.ReadAllLines(_iniPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                int index = line.IndexOf('=');
                string key;
                string originalKey;
                string value;
                if (index >= 0)
                {
                    originalKey = line.Substring(0, index).Trim();
                    key = originalKey.ToLower(CultureInfo.InvariantCulture);
                    value = line.Substring(index + 1, line.Length - index - 1).Trim();
                }
                else
                {
                    originalKey = line.Trim();
                    key = originalKey.ToLower(CultureInfo.InvariantCulture);
                    value = string.Empty;
                }
                if (key[0].Equals(';') || key[0].Equals('#') || key[0].Equals('/'))
                    continue;
                // Remove quotes
                if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                {
                    value = value.Substring(1, value.Length - 2);
                }
                if (string.IsNullOrEmpty(value) && key[0].Equals('[') && key[key.Length - 1].Equals(']'))
                {
                    sectionName = originalKey.Trim('[', ']');
                }
                else
                {
                    if (!string.IsNullOrEmpty(sectionName))
                    {
                        result.Append($"{sectionName}.{key}", value);
                    }
                    else
                    {
                        result.Append(key, value);
                    }
                }
            }
            return result;
        }

        public IConfigurationSet ReadConfigurationSet()
        {
            var result = Configuration.CreateSet();
            string sectionName = null!;
            foreach (var line in File.ReadAllLines(_iniPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                int index = line.IndexOf('=');
                string key;
                string originalKey;
                string value;
                if (index >= 0)
                {
                    originalKey = line.Substring(0, index).Trim();
                    key = originalKey.ToLower(CultureInfo.InvariantCulture);
                    value = line.Substring(index + 1, line.Length - index - 1).Trim();
                }
                else
                {
                    originalKey = line.Trim();
                    key = originalKey.ToLower(CultureInfo.InvariantCulture);
                    value = string.Empty;
                }
                if (key[0].Equals(';') || key[0].Equals('#') || key[0].Equals('/'))
                    continue;
                // Remove quotes
                if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                {
                    value = value.Substring(1, value.Length - 2);
                }
                if (string.IsNullOrEmpty(value) && key[0].Equals('[') && key[key.Length - 1].Equals(']'))
                {
                    sectionName = originalKey.Trim('[', ']');
                }
                else
                {
                    if (!string.IsNullOrEmpty(sectionName))
                    {
                        var currentSection = (false == result.ContainsSection(sectionName)) ? result.CreateSection(sectionName) : result.GetSection(sectionName);
                        currentSection.Append(key, value);
                    }
                    else
                    {
                        result.Default.Append(key, value);
                    }
                }
            }
            return result;
        }
    }
}