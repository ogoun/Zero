using System.Collections.Generic;
using System.Linq;

namespace DOM.DSL.Tokens
{
    public class TBlockToken :
        TToken
    {
        public string Name { get; }
        public TToken Condition { get; }
        public IEnumerable<TToken> Body { get; }

        public TBlockToken(string name,
            TToken condition,
            IEnumerable<TToken> body)
        {
            Name = name;
            Condition = condition?.Clone();
            Body = body.Select(b => b.Clone()).ToArray();
        }

        public TBlockToken(IEnumerable<TToken> body)
        {
            Name = string.Empty;
            Condition = null;
            Body = body.Select(b => b.Clone()).ToArray();
        }

        public override TToken Clone()
        {
            return new TBlockToken(this.Name, this.Condition?.Clone(), this.Body?.Select(b => b.Clone()).ToArray());
        }

        public override TToken CloneLocal()
        {
            return Clone();
        }
    }
}
