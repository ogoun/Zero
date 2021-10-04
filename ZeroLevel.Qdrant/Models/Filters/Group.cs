using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.Qdrant.Models.Filters
{
    public class Group
         : Operand
    {
        private List<Operand> _items = new List<Operand>();
        public GroupOperator Operator { get; private set; }
        public Group(GroupOperator op)
        {
            Operator = op;
        }

        public Group AppendGroup(GroupOperator op)
        {
            var g = new Group(op);
            _items.Add(g);
            return g;
        }

        public Group AppendCondition(Condition condition)
        {
            _items.Add(condition);
            return this;
        }

        public override string ToJSON()
        {
            string op;
            switch (Operator)
            {
                case GroupOperator.MustNot: op = "must_not"; break;
                case GroupOperator.Must: op = "must"; break;
                default: op = "mushould"; break;
            }
            return $"\"{op}\": [{string.Join(",", _items.Select(i => i.ToJSON()))}]";
        }
    }
}
