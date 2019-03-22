using DOM.DSL.Contracts;

namespace DOM.DSL.Tokens
{
    /// <summary>
    /// Абстрактная единица шаблона
    /// </summary>
    public abstract class TToken : TCloneable
    {
        public abstract TToken Clone();
        /// <summary>
        /// Копия с установкой NextToken в null, для предотвращения циклических расчетов
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
