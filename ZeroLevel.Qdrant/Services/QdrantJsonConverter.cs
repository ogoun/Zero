using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroLevel.Qdrant.DataAttributes;
using ZeroLevel.Qdrant.Models;
using ZeroLevel.Services.ObjectMapping;
using ZeroLevel.Services.Reflection;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Qdrant.Services
{
    public class QdrantJsonConverter<T>
    {
        private static string KeywordToString(IMemberInfo member, object v)
        {
            var text = TypeHelpers.IsString(member.ClrType) ? v as string : v.ToString();
            if (string.IsNullOrEmpty(text))
            {
                return "null";
            }
            else
            {
                return $"\"{JsonEscaper.EscapeString(text)}\"";
            }
        }

        /*
       integer - 64-bit integer in the range -9223372036854775808 to 9223372036854775807.      array of long
       float - 64-bit floating point number.                                                   array of double
       keyword - string value.                                                                 array of strings
       geo - Geographical coordinates. Example: { "lon": 52.5200, "lat": 13.4050 }             array of lon&lat of double
        */
        private const string KYEWORD_TYPE = "keyword";
        private const string GEO_TYPE = "geo";
        private const string FLOAT_TYPE = "float";
        private const string INTEGER_TYPE = "integer";
        public string ToJson(T value)
        {
            var json = new StringBuilder();

            var map = TypeMapper.Create<T>();
            foreach (var member in map.Members)
            {
                var val = member.Getter(value);
                var type = KYEWORD_TYPE;

                var attributes = member.Original.GetCustomAttributes(typeof(QdrantAttribute), true);
                if (attributes != null && attributes.Any())
                {
                    var dataAttribute = attributes[0];
                    if (dataAttribute is KeywordAttribute)
                    {
                        type = KYEWORD_TYPE;
                    }
                    else if (dataAttribute is FloatAttribute)
                    {
                        type = FLOAT_TYPE;
                    }
                    else if (dataAttribute is IntegerAttribute)
                    {
                        type = INTEGER_TYPE;
                    }
                    else if (dataAttribute is GeoAttribute)
                    {
                        type = GEO_TYPE;
                    }
                }
                else
                {
                    var item_type = member.ClrType;
                    // autodetect type
                    if (TypeHelpers.IsArray(item_type))
                    {
                        item_type = item_type.GetElementType();
                    }
                    else if (TypeHelpers.IsEnumerable(item_type))
                    {
                        item_type = TypeHelpers.GetElementTypeOfEnumerable(item_type);
                    }
                    if (item_type == typeof(float) || item_type == typeof(double) || item_type == typeof(decimal))
                    {
                        type = FLOAT_TYPE;
                    }
                    else if (item_type == typeof(int) || item_type == typeof(long) || item_type == typeof(byte) ||
                        item_type == typeof(short) || item_type == typeof(uint) || item_type == typeof(ulong) ||
                        item_type == typeof(ushort))
                    {
                        type = INTEGER_TYPE;
                    }
                    else if (item_type == typeof(Location))
                    {
                        type = GEO_TYPE;
                    }
                }
                switch (type)
                {
                    case KYEWORD_TYPE:
                        if (TypeHelpers.IsEnumerable(member.ClrType) && TypeHelpers.IsString(member.ClrType) == false)
                        {
                            var arr = val as IEnumerable;
                            json.Append($"\"{member.Name}\": {{ \"type\": \"keyword\", \"value\": [ {string.Join(", ", E(arr).Select(v => KeywordToString(member, v)))}] }},");
                        }
                        else
                        {
                            json.Append($"\"{member.Name}\": {{ \"type\": \"keyword\", \"value\":{KeywordToString(member, val)} }},");
                        }
                        break;
                    case GEO_TYPE:
                        if (TypeHelpers.IsEnumerable(member.ClrType) && TypeHelpers.IsString(member.ClrType) == false)
                        {
                            var arr = val as IEnumerable;
                            json.Append($"\"{member.Name}\": {{ \"type\": \"geo\", \"value\": [ {string.Join(",", E(arr).Select(v => v as Location).Where(l => l != null).Select(l => $" {{ \"lon\":{l.lon.ConvertToString()}, \"lat\":{l.lat.ConvertToString()} }}"))}] }},");
                        }
                        else
                        {
                            Location l = val as Location;
                            if (l != null)
                            {
                                json.Append($"\"{member.Name}\": {{ \"type\": \"geo\", \"value\": {{ \"lon\":{l.lon.ConvertToString()}, \"lat\":{l.lat.ConvertToString()} }} }},");
                            }
                        }
                        break;
                    case FLOAT_TYPE:
                        if (TypeHelpers.IsEnumerable(member.ClrType) && TypeHelpers.IsString(member.ClrType) == false)
                        {
                            var arr = val as IEnumerable;
                            json.Append($"\"{member.Name}\": {{ \"type\": \"float\", \"value\": [ {string.Join(",", E(arr).Select(v => Convert.ToDouble(v).ConvertToString()))}] }},");
                        }
                        else
                        {

                            json.Append($"\"{member.Name}\": {{ \"type\": \"float\", \"value\": {Convert.ToDouble(val).ConvertToString()} }},");
                        }
                        break;
                    case INTEGER_TYPE:
                        if (TypeHelpers.IsEnumerable(member.ClrType) && TypeHelpers.IsString(member.ClrType) == false)
                        {
                            var arr = val as IEnumerable;
                            json.Append($"\"{member.Name}\": {{ \"type\": \"integer\", \"value\": [ {string.Join(",", E(arr).Select(v => Convert.ToInt64(v)))}] }},");
                        }
                        else
                        {
                            json.Append($"\"{member.Name}\": {{ \"type\": \"integer\", \"value\": {Convert.ToInt64(val)}  }},");
                        }
                        break;
                }
            }
            if (json[json.Length - 1] == ',')
            {
                json.Remove(json.Length - 1, 1);
            }
            return json.ToString();
        }

        public IEnumerable<object> E(IEnumerable e)
        {
            if (e != null)
            {
                foreach (var i in e)
                {
                    yield return i;
                }
            }
        }
    }
}
