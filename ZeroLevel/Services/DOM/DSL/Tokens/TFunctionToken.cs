using System.Collections.Generic;
using System.Linq;

namespace DOM.DSL.Tokens
{
    public class TFunctionToken :
        TToken
    {
        public string FunctionName;
        public IEnumerable<TToken> FunctionArgs;
        public TToken NextToken;

        public override TToken Clone()
        {
            return new TFunctionToken
            {
                FunctionArgs = FunctionArgs.Select(a => a.Clone()).ToArray(),
                FunctionName = this.FunctionName,
                NextToken = this.NextToken?.Clone()
            };
        }

        public override TToken CloneLocal()
        {
            return new TFunctionToken
            {
                FunctionArgs = FunctionArgs.Select(a => a.Clone()).ToArray(),
                FunctionName = this.FunctionName,
                NextToken = null
            };
        }
    }
}