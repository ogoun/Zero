namespace DOM.DSL.Tokens
{
    public class TSystemToken :
        TToken
    {
        public string Command;
        public TToken Arg;

        public override TToken Clone()
        {
            return new TSystemToken
            {
                Command = this.Command,
                Arg = this.Arg?.Clone()
            };
        }

        public override TToken CloneLocal()
        {
            return Clone();
        }
    }
}
