using System.Collections.Generic;

namespace ConfigurationTests
{
    public class AppConfig
    {
        public string Url;
        public int BatchSize;
        public IEnumerable<string> Sheme;
        public IEnumerable<int> Port;
        public ServiceConfig Service;
    }

    public class ServiceConfig
    {
        public string AppName;
        public string AppKey;
        public string ServiceGroup;
        public string ServiceType;
    }
}
