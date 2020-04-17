using System.Collections.Generic;
using Xunit;

namespace ZeroLevel.UnitTests
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

    public class ConfigurationTest
    {
        [Fact]
        public void BindConfigurationTest()
        {
            // Arrange
            var set = Configuration.CreateSet();
            set.Default.Append("url", "https://habr.ru");
            set.Default.Append("batchSize", "1000");
            set.Default.Append("sheme", "socks");
            set.Default.Append("sheme", "http");
            set.Default.Append("sheme", "https");
            set.Default.Append("port", "80");
            set.Default.Append("port", "90");
            set.Default.Append("port", "8800");
            set.Default.Append("list", "1-5,7,9");
            var section = set.CreateSection("service");
            section.Append("AppName", "TestApp");
            section.Append("AppKey", "test.app");
            section.Append("ServiceGroup", "System");
            section.Append("ServiceType", "service");

            // Act
            var config = set.Bind<AppConfig>();

            // Assert
            Assert.Equal("https://habr.ru", config.Url);
            Assert.Equal(1000, config.BatchSize);
            Assert.Contains(config.Sheme, t=>t.Equals("socks"));
            Assert.Contains(config.Sheme, t => t.Equals("http"));
            Assert.Contains(config.Sheme, t => t.Equals("https"));

            Assert.Contains(config.Port, t => t == 80);
            Assert.Contains(config.Port, t => t == 90);
            Assert.Contains(config.Port, t => t == 8800);

            Assert.Contains(config.List, i => i == 1);
            Assert.Contains(config.List, i => i == 2);
            Assert.Contains(config.List, i => i == 3);
            Assert.Contains(config.List, i => i == 4);
            Assert.Contains(config.List, i => i == 5);
            Assert.Contains(config.List, i => i == 7);
            Assert.Contains(config.List, i => i == 9);

            Assert.DoesNotContain(config.List, i => i == 8);
            Assert.DoesNotContain(config.List, i => i == 6);

            Assert.Equal("test.app", config.Service.AppKey);
            Assert.Equal("TestApp", config.Service.AppName);
            Assert.Equal("System", config.Service.ServiceGroup);
            Assert.Equal("service", config.Service.ServiceType);
        }
    }
}
