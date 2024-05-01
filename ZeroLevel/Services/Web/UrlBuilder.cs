using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;

namespace ZeroLevel.Services.Web
{
    public class UrlBuilder
    {
        private readonly Func<object, string> _serializer;

        public UrlBuilder(Func<object, string> serializer)
        {
            _serializer = serializer;
        }

        public Uri BuildRequestUrlFromDTO<T>(string baseUri, string resource, T instance)
        {
            if (null == instance)
            {
                return BuildRequestUrl(baseUri, resource, null!);
            }
            var members = typeof(T).GetMembers(
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy |
                BindingFlags.GetField |
                BindingFlags.GetProperty |
                BindingFlags.Instance);
            var parameters = new Dictionary<string, object>();
            foreach (var member in members)
            {
                switch (member.MemberType)
                {
                    case MemberTypes.Property:
                        parameters.Add(member.Name.ToLowerInvariant(), (member as PropertyInfo)!.GetValue(instance));
                        break;
                    case MemberTypes.Field:
                        parameters.Add(member.Name.ToLowerInvariant(), (member as FieldInfo)!.GetValue(instance));
                        break;
                    default:
                        continue;
                }
            }
            return BuildRequestUrl(baseUri, resource, parameters);
        }

        public Uri BuildRequestUrl(string baseUri, string resource, IDictionary<string, object> parameters)
        {
            if (null == resource) throw new ArgumentNullException("resource");
            var stringBuilder = new StringBuilder(baseUri);
            if (baseUri[baseUri.Length - 1] != '/')
                stringBuilder.Append('/');
            if (resource[0] != '/')
            {
                stringBuilder.Append(resource);
            }
            else
            {
                stringBuilder.Append(resource.Substring(1));
            }
            if (parameters != null && parameters.Count > 0)
            {
                stringBuilder.Append("?");
                foreach (var pair in parameters)
                {
                    if (pair.Value == null!)
                    {
                        stringBuilder.Append(pair.Value);
                    }
                    else
                    {
                        var vtype = pair.Value.GetType();
                        if (vtype.IsArray)
                        {
                            if (vtype.GetElementType() == typeof(string))
                            {
                                var arr = (string[])pair.Value;
                                stringBuilder.Append(string.Join("&", arr.Select(i => $"{HttpUtility.UrlEncode(pair.Key)}[]={HttpUtility.UrlEncode(i)}")));
                            }
                            else
                            {
                                var arr = (object[])pair.Value;
                                stringBuilder.Append(string.Join("&", arr.Select(i => $"{HttpUtility.UrlEncode(pair.Key)}[]={HttpUtility.UrlEncode(_serializer(i))}")));
                            }
                        }
                        else
                        {
                            if (vtype == typeof(string))
                            {
                                stringBuilder.Append($"{HttpUtility.UrlEncode(pair.Key)}={HttpUtility.UrlEncode((string)pair.Value)}");
                            }
                            else
                            {
                                stringBuilder.Append($"{HttpUtility.UrlEncode(pair.Key)}={HttpUtility.UrlEncode(_serializer(pair.Value))}");
                            }
                        }
                    }
                    stringBuilder.Append("&");
                }
            }
            return new Uri(stringBuilder.ToString().TrimEnd('&'));
        }
    }
}
