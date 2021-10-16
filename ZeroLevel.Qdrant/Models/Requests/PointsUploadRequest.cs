using System;
using System.Linq;
using System.Text;
using ZeroLevel.Qdrant.Services;
using ZeroLevel.Services.Collections;

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
        public double[] vector { get; set; }
    }
    public sealed class UpsertPoints<T>
    {
        public UpsertPoint<T>[] points { get; set; }
    }
    public sealed class PointsUploadRequest<T>
    {
        private static IEverythingStorage _cachee = EverythingStorage.Create();
        public UpsertPoints<T> upsert_points { get; set; }

        public string ToJSON()
        {
            if (!_cachee.ContainsKey<QdrantJsonConverter<T>>("converter"))
            {
                _cachee.Add("converter", new QdrantJsonConverter<T>());
            }
            var converter = _cachee.Get<QdrantJsonConverter<T>>("converter");

            Func<UpsertPoint<T>, string> p_conv = up =>
            {
                if (up.id != null)
                {
                    return $"\"id\": {up.id}, \"payload\": {{ {converter.ToJson(up.payload)} }}, \"vector\": [{ string.Join(",", up.vector.Select(f => f.ConvertToString()))}]";
                }
                return $"\"payload\": {{ {converter.ToJson(up.payload)} }}, \"vector\": [{ string.Join(",", up.vector.Select(f => f.ConvertToString()))}]";
            };

            var json = new StringBuilder();
            json.Append("{");
            json.Append("\"upsert_points\": {");
            json.Append("\"points\":[ {");
            json.Append(string.Join("},{", upsert_points.points.Select(p => p_conv(p))));
            json.Append("}]");
            json.Append("}");
            json.Append("}");
            return json.ToString();
        }
    }

    public sealed class ColumnPoints<T>
    {
        public long[] ids { get; set; }
        public T[] payloads { get; set; }
        public double[,] vectors { get; set; }
    }

    public sealed class UpsertColumnPoints<T>
    {
        public ColumnPoints<T> batch { get; set; }
    }

    public sealed class PointsColumnUploadRequest<T>
    {
        private static IEverythingStorage _cachee = EverythingStorage.Create();
        public UpsertColumnPoints<T> upsert_points { get; set; }

        public string ToJSON()
        {
            if (!_cachee.ContainsKey<QdrantJsonConverter<T>>("converter"))
            {
                _cachee.Add("converter", new QdrantJsonConverter<T>());
            }
            var converter = _cachee.Get<QdrantJsonConverter<T>>("converter");

            var json = new StringBuilder();
            json.Append("{");
            json.Append("\"upsert_points\": {");
            json.Append("\"batch\": {");
            if (upsert_points.batch.ids != null && upsert_points.batch.ids.Length > 0)
            {
                json.Append($"\"ids\": [{string.Join(",", upsert_points.batch.ids)}], ");
            }
            json.Append($"\"payloads\": [ {{ {string.Join("} ,{ ", upsert_points.batch.payloads.Select(payload => converter.ToJson(payload)))} }} ], ");
            json.Append($"\"vectors\": [{string.Join(",", Enumerable.Range(0, upsert_points.batch.vectors.GetLength(0)).Select(row => "[" + string.Join(",", ArrayExtensions.GetRow(upsert_points.batch.vectors, row).Select(f => f.ConvertToString())) + "]"))}]");
            json.Append("}");
            json.Append("}");
            json.Append("}");
            return json.ToString();
        }
    }
}
