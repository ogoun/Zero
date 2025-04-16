using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ZeroLevel.Services.Formats.YAML
{
    public static class YamlToJsonConverter
    {
        public static string Convert(string yaml)
        {
            if (string.IsNullOrEmpty(yaml))
            {
                return "{}";
            }

            yaml = RemoveComments(yaml);
            Dictionary<string, object> yamlData = ParseYaml(yaml, 0);
            return ConvertToJson(yamlData);
        }

        private static string RemoveComments(string yaml)
        {
            return Regex.Replace(yaml, @"#.*", string.Empty);
        }

        private static Dictionary<string, object> ParseYaml(string yaml, int indentLevel)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            string[] lines = yaml.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string currentKey = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                int currentLineIndent = CountLeadingSpaces(lines[i]);

                if (currentLineIndent < indentLevel)
                {
                    break;
                }
                if (currentLineIndent > indentLevel)
                {
                    continue;
                }

                string trimmedLine = line.TrimStart();

                if (trimmedLine.StartsWith("-"))
                {
                    if (!data.ContainsKey(currentKey))
                    {
                        data[currentKey] = new List<object>();
                    }
                    List<object> list = (List<object>)data[currentKey];
                    string listValue = trimmedLine.Substring(1).Trim();
                    if (listValue.StartsWith("{"))
                    {
                        listValue = listValue.Substring(1, listValue.Length - 2).Trim();
                        string[] entries = listValue.Split(',');
                        Dictionary<string, object> inlineObj = new Dictionary<string, object>();
                        foreach (string entry in entries)
                        {
                            string[] kvp = entry.Trim().Split(':');
                            inlineObj.Add(kvp[0].Trim(), kvp[1].Trim());
                        }
                        list.Add(inlineObj);

                    }
                    else if (listValue.StartsWith("["))
                    {
                        listValue = listValue.Substring(1, listValue.Length - 2).Trim();
                        string[] entries = listValue.Split(',');
                        List<object> inlineList = new List<object>();
                        foreach (string entry in entries)
                        {
                            inlineList.Add(entry.Trim());
                        }
                        list.Add(inlineList);
                    }

                    else
                    {
                        list.Add(GetValue(listValue));
                    }


                }
                else
                {
                    string[] parts = trimmedLine.Split(new[] { ':' }, 2);

                    if (parts.Length == 2)
                    {
                        currentKey = parts[0].Trim();
                        string value = parts[1].Trim();

                        if (string.IsNullOrEmpty(value))
                        {
                            int nextIndentLevel = int.MaxValue;
                            for (int j = i + 1; j < lines.Length; j++)
                            {
                                int tempIndent = CountLeadingSpaces(lines[j]);
                                if (tempIndent > currentLineIndent)
                                {
                                    nextIndentLevel = tempIndent;
                                    break;
                                }
                            }

                            StringBuilder subYaml = new StringBuilder();
                            for (int j = i + 1; j < lines.Length; j++)
                            {
                                if (CountLeadingSpaces(lines[j]) >= nextIndentLevel)
                                {
                                    subYaml.AppendLine(lines[j]);

                                }
                                else
                                {
                                    break;
                                }
                            }

                            data[currentKey] = ParseYaml(subYaml.ToString(), nextIndentLevel);
                            i += subYaml.ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;

                        }

                        else if (value.StartsWith("{"))
                        {
                            value = value.Substring(1, value.Length - 2).Trim();
                            string[] entries = value.Split(',');
                            Dictionary<string, object> inlineObj = new Dictionary<string, object>();
                            foreach (string entry in entries)
                            {
                                string[] kvp = entry.Trim().Split(':');
                                inlineObj.Add(kvp[0].Trim(), kvp[1].Trim());
                            }
                            data.Add(currentKey, inlineObj);
                        }
                        else if (value.StartsWith("["))
                        {
                            value = value.Substring(1, value.Length - 2).Trim();
                            string[] entries = value.Split(',');
                            List<object> inlineList = new List<object>();
                            foreach (string entry in entries)
                            {
                                inlineList.Add(entry.Trim());
                            }
                            data.Add(currentKey, inlineList);

                        }

                        else
                        {
                            data[currentKey] = GetValue(value);
                        }

                    }
                }
            }
            return data;
        }

        private static object GetValue(string value)
        {
            if (value.ToLower() == "true")
            {
                return true;
            }
            if (value.ToLower() == "false")
            {
                return false;
            }
            if (value.ToLower() == "null")
            {
                return null;
            }
            if (int.TryParse(value, out int intValue))
            {
                return intValue;
            }
            if (double.TryParse(value, out double doubleValue))
            {
                return doubleValue;
            }
            return value;
        }

        private static string ConvertToJson(Dictionary<string, object> data)
        {
            StringBuilder json = new StringBuilder("{");
            bool first = true;

            foreach (KeyValuePair<string, object> pair in data)
            {
                if (!first)
                {
                    json.Append(",");
                }

                json.Append($"\"{pair.Key}\":");

                if (pair.Value is Dictionary<string, object> subData)
                {
                    json.Append(ConvertToJson(subData));
                }
                else if (pair.Value is List<object> listData)
                {
                    json.Append(ConvertListToJson(listData));
                }
                else if (pair.Value is string)
                {
                    json.Append($"\"{pair.Value}\"");
                }
                else if (pair.Value is bool || pair.Value is int || pair.Value is double || pair.Value == null)
                {
                    json.Append($"{pair.Value.ToString().ToLower()}");
                }

                first = false;
            }

            json.Append("}");
            return json.ToString();
        }
        private static string ConvertListToJson(List<object> list)
        {
            StringBuilder json = new StringBuilder("[");
            bool first = true;
            foreach (object item in list)
            {
                if (!first)
                {
                    json.Append(",");
                }
                if (item is Dictionary<string, object>)
                {
                    json.Append(ConvertToJson((Dictionary<string, object>)item));
                }
                else if (item is List<object>)
                {
                    json.Append(ConvertListToJson((List<object>)item));
                }
                else if (item is string)
                {
                    json.Append($"\"{item}\"");
                }
                else if (item is bool || item is int || item is double || item == null)
                {
                    json.Append($"{item.ToString().ToLower()}");
                }

                first = false;
            }
            json.Append("]");
            return json.ToString();
        }

        private static int CountLeadingSpaces(string line)
        {
            int count = 0;
            foreach (char c in line)
            {
                if (c == ' ')
                {
                    count++;
                }
                else
                {
                    break;
                }
            }
            return count;
        }
    }
}
