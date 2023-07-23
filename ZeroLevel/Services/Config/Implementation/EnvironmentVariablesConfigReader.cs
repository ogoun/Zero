using System;

namespace ZeroLevel.Services.Config.Implementation
{
    internal sealed class EnvironmentVariablesConfigReader
        : IConfigurationReader
    {
        public IConfiguration ReadConfiguration()
        {
            var result = Configuration.Create();
            var enumerator = Environment.GetEnvironmentVariables().GetEnumerator();
            while (enumerator.MoveNext())
            {
                string key = (string)enumerator.Entry.Key;
                string value = ((string)enumerator.Entry.Value) ?? string.Empty;
                result.Append(key, value);
            }
            return result;
        }

        public IConfigurationSet ReadConfigurationSet()
        {
            return Configuration.CreateSet(ReadConfiguration());
        }
    }
}
