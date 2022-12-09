namespace ZeroLevel.Qdrant.Models.Requests
{
    public enum IndexFieldType
    {
        Keyword,
        Integer,
        Float,
        Geo
    }

    /// <summary>
    /// Available field types are:
    /// keyword - for keyword payload, affects Match filtering conditions.
    /// integer - for integer payload, affects Match and Range filtering conditions.
    /// float - for float payload, affects Range filtering conditions.
    /// geo - for geo payload, affects Geo Bounding Box and Geo Radius filtering conditions.
    /// </summary>
    internal sealed class CreateIndexRequest
    {
        public string field_name { get; set; }
        public string field_type { get; set; }
        public CreateIndexRequest(string name, IndexFieldType type)
        { 
            field_name = name;
            switch (type)
            {
                case IndexFieldType.Integer: field_type = "integer"; break;
                case IndexFieldType.Float: field_type = "float"; break;
                case IndexFieldType.Geo: field_type = "geo"; break;
                case IndexFieldType.Keyword: field_type = "keyword"; break;
            }
        }
    }
}
