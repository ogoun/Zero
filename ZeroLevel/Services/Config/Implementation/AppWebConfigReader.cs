using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ZeroLevel.Services.Config.Implementation
{
    internal sealed class AppWebConfigReader
    {
        private readonly string _configFilePath;

        internal AppWebConfigReader(string configFilePath = null)
        {
            if (configFilePath == null)
            {
                var appConfig = Path.Combine(Configuration.BaseDirectory, $"{System.AppDomain.CurrentDomain.FriendlyName}.config");
                if (File.Exists(appConfig))
                {
                    _configFilePath = appConfig;
                }
                else
                {
                    var webConfig = Path.Combine(Configuration.BaseDirectory, "web.config");
                    if (File.Exists(webConfig))
                    {
                        _configFilePath = webConfig;
                    }
                    else
                    {
                        _configFilePath = Directory.GetFiles(Configuration.BaseDirectory, "*.config").FirstOrDefault();
                    }
                }
            }
            else
            {
                if (configFilePath.IndexOf(':') < 0)
                {
                    this._configFilePath = Path.Combine(Configuration.BaseDirectory, configFilePath);
                }
                else
                {
                    this._configFilePath = configFilePath;
                }
            }
        }

        internal IEnumerable<string> GetSections()
        {
            if (_configFilePath != null)
            {
                var xdoc = XDocument.Load(_configFilePath);
                var cs = xdoc.Descendants("connectionStrings").
                    Select(x => x.Name.LocalName);
                return xdoc.Descendants("section")
                  .Select(x => x.Attribute("name").Value).Union(cs);
            }
            return Enumerable.Empty<string>();
        }

        internal IEnumerable<Tuple<string, string>> ReadSection(string sectionName)
        {
            if (_configFilePath != null)
            {
                var xdoc = XDocument.Load(_configFilePath);
                return xdoc.Descendants(sectionName).
                    SelectMany(x => x.Nodes().Where(n => null != (n as XElement)).Select(n =>
                    {
                        var xe = n as XElement;
                        return new Tuple<string, string>(FindName(xe), FindValue(xe));
                    }));
            }
            return Enumerable.Empty<Tuple<string, string>>();
        }

        private static string FindName(XElement n)
        {
            var attributes = n.Attributes().
                ToDictionary(i => i.Name.LocalName.ToLowerInvariant(), j => j.Value);
            foreach (var v in new[] { "key", "name", "code", "id" })
            {
                if (attributes.ContainsKey(v))
                    return attributes[v];
            }
            return n.Name.LocalName;
        }

        private static string FindValue(XElement n)
        {
            var attributes = n.Attributes().
                ToDictionary(i => i.Name.LocalName.ToLowerInvariant(), j => j.Value);
            foreach (var v in new[] { "value", "val", "file", "db", "connectionstring" })
            {
                if (attributes.ContainsKey(v))
                    return attributes[v];
            }
            return n.Value;
        }

        internal IEnumerable<Tuple<string, string>> ReadAppSettings()
        {
            return ReadSection("appSettings");
        }
    }
}