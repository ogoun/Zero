using DOM.DSL.Contracts;

namespace DOM.DSL.Tokens
{
    /// <summary>
    /// Abstract token
    /// </summary>
    public abstract class TToken : TCloneable
    {
        public abstract TToken Clone();

        /// <summary>
        /// Copying token with set NextToken to null, to break cycle
        /// </summary>
        /// <returns></returns>
        public abstract TToken CloneLocal();

        public TElementToken AsElementToken() => this as TElementToken;

        public TFunctionToken AsFunctionToken() => this as TFunctionToken;

        public TTextToken AsTextToken() => this as TTextToken;

        public TBlockToken AsBlockToken() => this as TBlockToken;

        public TPropertyToken AsPropertyToken() => this as TPropertyToken;

        public TSystemToken AsSystemToken() => this as TSystemToken;
    }
}