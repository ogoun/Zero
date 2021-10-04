using ZeroLevel.Qdrant.Models.Filters;
using System;
using System.Linq;
using System.Text;

namespace ZeroLevel.Qdrant.Models.Requests
{
    internal sealed class SearchRequest
    {
        /// <summary>
        /// Look only for points which satisfies this conditions
        /// </summary>
        public Filter Filter { get; set; }
        /// <summary>
        /// Look for vectors closest to this        
        /// </summary>
        public double[] FloatVector { get; set; }
        public long[] IntegerVector { get; set; }
        /// <summary>
        /// Max number of result to return
        /// </summary>
        public uint Top { get; set; }
        /// <summary>
        /// Params relevant to HNSW index /// Size of the beam in a beam-search. Larger the value - more accurate the result, more time required for search.
        /// </summary>
        public uint? HNSW { get; set; } = null;


        /*
         
{
    "filter": {
        "must": [
            {
                "key": "city",
                "match": {
                    "keyword": "London"
                }
            }
        ]
    },
    "params": {
        "hnsw_ef": 128
    },
    "vector": [0.2, 0.1, 0.9, 0.7],
    "top": 3
}
        
         */
        public string ToJson()
        {
            var json = new StringBuilder();
            json.Append("{");
            if (Filter == null || Filter.IsEmpty)
            {
                json.Append("\"filter\": null,");
            }
            else
            {
                json.Append(Filter.ToJSON());
                json.Append(',');
            }
            if (HNSW != null)
            {
                json.Append($"\"params\": {{ \"hnsw_ef\": {HNSW.Value} }},");
            }
            if (FloatVector != null)
            {
                json.Append($"\"vector\": [{string.Join(",", FloatVector.Select(f => f.ConvertToString()))}],");
            }
            else if (IntegerVector != null)
            {
                json.Append($"\"vector\": [{string.Join(",", IntegerVector)}],");
            }
            else
            {
                throw new ArgumentException("No one vectors is set");
            }
            json.Append($"\"top\": {Top}");
            json.Append("}");
            return json.ToString();
        }
    }
}
