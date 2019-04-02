using System;
using System.Globalization;

namespace ZeroLevel.Services.Config.Implementation
{
    internal sealed class CommandLineReader 
        : IConfigurationReader
    {
        private readonly string[] _args;

        public CommandLineReader(string[] args)
        {
            _args = args;
        }

        public IConfiguration ReadConfiguration()
        {
            var result = Configuration.Create();
            if (_args != null)
            {
                try
                {
                    foreach (string arg in _args)
                    {
                        int index = arg.IndexOf('=');
                        string key;
                        string value;
                        if (index >= 0)
                        {
                            key = arg.Substring(0, index).TrimStart('/').Trim().ToLower(CultureInfo.CurrentCulture);
                            value = arg.Substring(index + 1, arg.Length - index - 1).Trim(' ', '"');
                        }
                        else
                        {
                            key = arg.TrimStart('-', '/').Trim().ToLower(CultureInfo.CurrentCulture);
                            value = string.Empty;
                        }
                        result.Append(key, value);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Can't read configuration from command line arguments", ex);
                }
            }
            return result;
        }

        public IConfigurationSet ReadConfigurationSet()
        {
            return Configuration.CreateSet(ReadConfiguration());
        }
    }
}
