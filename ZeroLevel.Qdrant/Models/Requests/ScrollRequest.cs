using System;
using System.Text;
using ZeroLevel.Qdrant.Models.Filters;

namespace ZeroLevel.Qdrant.Models.Requests
{
    internal sealed class ScrollRequest
    {
        public Filter Filter { get; set; }
        public bool WithPayload { get; set; } = true;
        public bool WithVector { get; set; } = true;
        public long Limit { get; set; }
        public long Offset { get; set; }

        /*
         {
    "filter": {
        "must": [
            { "has_id": [0, 3, 100] }
        ]
    },    
    "limit": 10000,
    "offset": 0,
    "with_payload": false,
    "with_vector": true
}
         */

        public string ToJson()
        {
            var json = new StringBuilder();
            json.Append("{");
            if (Filter == null || Filter.IsEmpty)
            {
                throw new ArgumentException("Filter must not by null or empty");
            }
            else
            {
                json.Append(Filter.ToJSON());
                json.Append(',');
            }
            json.Append($"\"limit\": {Limit},");
            json.Append($"\"offset\": {Offset},");
            json.Append($"\"with_payload\": {WithPayload.ToString().ToLowerInvariant()},");
            json.Append($"\"with_vector\": {WithVector.ToString().ToLowerInvariant()}");
            json.Append("}");
            return json.ToString();

        }
    }
}
