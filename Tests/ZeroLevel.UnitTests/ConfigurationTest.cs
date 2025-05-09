﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using ZeroLevel.Services.Config;

namespace ZeroLevel.UnitTests
{
    public class Address
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }
    public class AddressParser
        : IConfigRecordParser
    {
        public object Parse(string line)
        {
            var parts = line.Split(';').Where(part => string.IsNullOrWhiteSpace(part) == false).ToArray();
            var addresses = new Address[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var hp = parts[i].Trim().Split(':');
                addresses[i] = new Address { Host = hp[0], Port = int.Parse(hp[1]) };
            }
            return addresses;
        }
    }

    public class AppConfig
    {
        public string Url;
        public int BatchSize;
        public IEnumerable<string> Sheme;
        public int[] Port;
        public ServiceConfig Service;
        public IEnumerable<int> List;
        [ConfigRecordParse(typeof(AddressParser))]
        public Address[] Hosts;
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
            set.Default.Append("hosts", "host1:8800;host2:122;host3:1744");

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
            Assert.Contains(config.Sheme, t => t.Equals("socks"));
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

        [Fact]
        public void NumbersTest()
        {
            // Arrange
            var set = Configuration.CreateSet();
            var d = 0.27;
            var f = 0.5f;
            var i = 12;

            // Act
            set.Default.Append("d", "0.27");
            set.Default.Append("f", "0.5");
            set.Default.Append("i", "12");

            var td = set.Default.First<double>("d");

            // Assert
            Assert.Equal(d, td);
            Assert.Equal(f, set.Default.First<float>("f"));
            Assert.Equal(i, set.Default.First<int>("i"));
        }

        /*
{
  "url": "http://tes.io",
  "port": 9091,
  "limits": {
    "min": {
      "cpu": 20,
      "gpu": 0,
      "mem": 200
    },
    "max": {
      "cpu": 40,
      "gpu": 50,
      "mem": 600
    },
    "optional": null,
    "persistent": true
  }
}
         */
        [Fact]
        public void JsonConfigTest()
        {
            // Arrange
            var json = ZeroLevel.UnitTests.Properties.Resources.test_json_config;
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            File.WriteAllBytes(tempFilePath, json);

            // Act
            IConfigurationSet set;
            try
            {
                set = Configuration.ReadSetFromJsonFile(tempFilePath);
            }
            finally
            {
                File.Delete(tempFilePath);
            }

            // Assert
            Assert.Equal("http://tes.io", set.Default.First("url"));
            Assert.Equal(9091, set.Default.First<int>("port"));
            Assert.Equal(20, set["limits.min"].First<int>("cpu"));
            Assert.Equal(0, set["limits.min"].First<int>("gpu"));
            Assert.Equal(200, set["limits.min"].First<int>("mem"));
            Assert.Equal(40, set["limits.max"].First<int>("cpu"));
            Assert.Equal(50, set["limits.max"].First<int>("gpu"));
            Assert.Equal(600, set["limits.max"].First<int>("mem"));
            Assert.Equal(string.Empty, set["limits"].First("optional"));
            Assert.Equal(true, set["limits"].First<bool>("persistent"));
        }


        /*
---
apiVersion: extensions/v1beta1
kind: Ingress
metadata:
  name: my-ingress
spec:
  rules:
  - host: my-app.s<номер своего логина>.edu.slurm.io
    http:
      paths:
      - backend:
          serviceName: my-service
          servicePort: 80
...        
        */
        [Fact]
        public void YamlConfigTest()
        {
            // Arrange
            var json = ZeroLevel.UnitTests.Properties.Resources.test_yaml_config;
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            File.WriteAllBytes(tempFilePath, json);

            // Act
            IConfigurationSet set;
            try
            {
                set = Configuration.ReadSetFromYamlFile(tempFilePath);
            }
            finally
            {
                File.Delete(tempFilePath);
            }

            // Assert
            Assert.Equal("extensions/v1beta1", set.Default.First("apiVersion"));
            Assert.Equal("Ingress", set.Default.First("kind"));
            Assert.Equal("my-ingress", set["metadata"].First("name"));
            Assert.Equal("my-app.s<номер своего логина>.edu.slurm.io", set["spec.rules.0"].First("host"));
            Assert.Equal("my-service", set["spec.rules.0.http.paths.0.backend"].First("serviceName"));
            Assert.Equal(80, set["spec.rules.0.http.paths.0.backend"].First<int>("servicePort"));
        }


        [Fact]
        public void MergeTest()
        {
            // ARRANGE
            var set1 = Configuration.CreateSet();
            set1.Default.Append("dk1", "1");
            set1.Default.Append("dk1", "1");
            set1["A"].Append("Ak1", "ak1");
            set1["A"].Append("Ak2", "ak2");
            var set2 = Configuration.CreateSet();
            set2.Default.Append("dk1", "2");
            set2["A"].Append("Ak1", "ak1");
            var set3 = Configuration.CreateSet();
            set3.Default.Append("dk1", "3");
            set3["A"].Append("Ak1", "ak2");

            // ACT 1
            var mergedSet1 = Configuration.Merge(Services.Config.ConfigurationRecordExistBehavior.Append, set1, set2, set3);
            // ASSERT 1
            Assert.Equal(mergedSet1.Default["dk1"].Count(), 4);
            Assert.Contains(mergedSet1.Default["dk1"], i => i == "1");
            Assert.Contains(mergedSet1.Default["dk1"], i => i == "2");
            Assert.Contains(mergedSet1.Default["dk1"], i => i == "3");

            Assert.Equal(mergedSet1["A"]["Ak1"].Count(), 3);
            Assert.Equal(mergedSet1["A"]["Ak2"].Count(), 1);

            // ACT 2
            var mergedSet2 = Configuration.Merge(Services.Config.ConfigurationRecordExistBehavior.IgnoreNew, set1, set2, set3);
            // ASSERT 2
            Assert.Equal(mergedSet2.Default["dk1"].Count(), 2);
            Assert.Contains(mergedSet2.Default["dk1"], i => i == "1");

            Assert.Equal(mergedSet2["A"]["Ak1"].Count(), 1);
            Assert.Equal(mergedSet2["A"]["Ak2"].Count(), 1);

            // ACT 3
            var mergedSet3 = Configuration.Merge(Services.Config.ConfigurationRecordExistBehavior.Overwrite, set1, set2, set3);
            // ASSERT 3
            Assert.Equal(mergedSet3.Default["dk1"].Count(), 1);
            Assert.Contains(mergedSet3.Default["dk1"], i => i == "3");

            Assert.Equal(mergedSet3["A"]["Ak1"].Count(), 1);
            Assert.Equal(mergedSet3["A"]["Ak2"].Count(), 1);
        }
    }
}
