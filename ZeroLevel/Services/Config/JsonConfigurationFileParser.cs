using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ZeroLevel.Services.Config
{
    /// <summary>
    /// Edited version from Microsoft.Extensions.Configuration
    /// </summary>
    internal sealed class JsonConfigurationFileParser
    {
        private JsonConfigurationFileParser() { }

        private readonly Dictionary<string, Dictionary<string, List<string>>> _data = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<string> _paths = new Stack<string>();

        public static IDictionary<string, Dictionary<string, List<string>>> Parse(Stream input)
            => new JsonConfigurationFileParser().ParseStream(input);

        private Dictionary<string, Dictionary<string, List<string>>> ParseStream(Stream input)
        {
            var jsonDocumentOptions = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

            using (var reader = new StreamReader(input))
            using (JsonDocument doc = JsonDocument.Parse(reader.ReadToEnd(), jsonDocumentOptions))
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new FormatException($"Top-level JSON element must be an object. Instead, '{doc.RootElement.ValueKind}' was found.");
                }
                VisitObjectElement(doc.RootElement);
            }

            return _data;
        }

        private void VisitObjectElement(JsonElement element)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                EnterContext(property.Name);
                VisitValue(property.Value);
                ExitContext();
            }
        }

        private void VisitArrayElement(JsonElement element)
        {
            int index = 0;
            foreach (JsonElement arrayElement in element.EnumerateArray())
            {
                EnterContext(index.ToString());
                VisitValue(arrayElement);
                ExitContext();
                index++;
            }
        }

        private void VisitValue(JsonElement value)
        {
            Debug.Assert(_paths.Count > 0);

            switch (value.ValueKind)
            {
                case JsonValueKind.Object:
                    VisitObjectElement(value);
                    break;

                case JsonValueKind.Array:
                    VisitArrayElement(value);
                    break;

                case JsonValueKind.Number:
                case JsonValueKind.String:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    if (_data.ContainsKey(CurrentSection) == false)
                    {
                        _data[CurrentSection] = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    }
                    var data = _data[CurrentSection];

                    string key = _paths.Peek();
                    if (data.ContainsKey(key))
                    {
                        data[key].Add(value.ToString());
                    }
                    else
                    {
                        data[key] = new List<string> { value.ToString() };
                    }
                    break;

                default:
                    throw new FormatException($"Unsupported JSON token '{value.ValueKind}' was found.");
            }
        }

        public static readonly string KeyDelimiter = ".";

        private string CurrentSection = Configuration.DEFAULT_SECTION_NAME;
        private void EnterContext(string context)
        {
            if (_paths.Count > 0)
            {
                CurrentSection = string.Join(KeyDelimiter, _paths.Reverse());
            }
            else
            {
                CurrentSection = Configuration.DEFAULT_SECTION_NAME;
            }
            _paths.Push(context);
        }

        private void ExitContext() => _paths.Pop();
    }
}
