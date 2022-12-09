using System;
using System.Linq;
using System.Text;
using ZeroLevel.Qdrant.Services;

namespace ZeroLevel.Qdrant.Models.Requests
{
    /*
       integer - 64-bit integer in the range -9223372036854775808 to 9223372036854775807.      array of long
       float - 64-bit floating point number.                                                   array of double
       keyword - string value.                                                                 array of strings
       geo - Geographical coordinates. Example: { "lon": 52.5200, "lat": 13.4050 }             array of lon&lat of double
        */

    public sealed class UpsertPoint<T>
    {
        public long? id { get; set; } = null;
        public T payload { get; set; }
        public float[] vector { get; set; }
    }

    public sealed class PointsUpsertRequest<T>
    {
        public UpsertPoint<T>[] points { get; set; }

        public string ToJSON()
        {
            if (points != null && points.Length > 0)
            {
                var dims = points[0].vector.Length;
                Func<T, string> converter = o => QdrantJsonConverter<T>.ConvertToJson(o);
                var json = new StringBuilder();
                json.Append("{");
                json.Append("\"batch\": {");
                json.Append($"\"ids\": [{string.Join(",", points.Select(p => p.id))}], ");
                json.Append($"\"payloads\": [ {{ {string.Join("} ,{ ", points.Select(p => converter(p.payload)))} }} ], ");
                json.Append($"\"vectors\": [{string.Join(", ", points.Select(p => QdrantJsonConverter<T>.ConvertToJson(p.vector)))}]");
                json.Append("}");
                json.Append("}");
                return json.ToString();
            }
            return String.Empty;
        }

    }
}
