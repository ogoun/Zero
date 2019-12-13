using System.Collections.Generic;

namespace ConfigurationTests
{
    public class AppConfig
    {
        public string Url;
        public int BatchSize;
        public IEnumerable<string> Sheme;
        public int[] Port;
        public ServiceConfig Service;
        public IEnumerable<int> List;
    }

    public class ServiceConfig
    {
        public string AppName;
        public string AppKey;
        public string ServiceGroup;
        public string ServiceType;
    }
}
