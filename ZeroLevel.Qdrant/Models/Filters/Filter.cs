using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroLevel.Qdrant.Models.Filters
{
    /// <summary>
    /// Filter for search in qdrant
    /// </summary>
    public class Filter
    {
        private List<Group> _groups = new List<Group>();

        public bool IsEmpty => _groups?.Count == 0;

        public Group AppendGroup(GroupOperator op)
        {
            var g = new Group(op);
            _groups.Add(g);
            return g;
        }

        public string ToJSON()
        {
            var json = new StringBuilder();
            json.Append("\"filter\": {");
            json.Append(string.Join(",", _groups.Select(g => g.ToJSON())));
            json.Append("}");
            return json.ToString();
        }
    }
}
