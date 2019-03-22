using DOM.DSL.Services;
using System.Collections.Generic;

namespace DOM.DSL.Contexts
{
    internal abstract class TContext
    {
        protected static HashSet<string> _blockNames = new HashSet<string> { "for", "if", "flow", "comm", "block" };
        protected static HashSet<string> _endblockNames = new HashSet<string> { "endfor", "endif", "endflow", "endcomm", "endblock" };

        protected static HashSet<string> _elementNames = new HashSet<string> { "text", "now", "nowutc", "guid",
            "id","summary","header","categories", "directions","author","copyright","created",
            "lang","priority","source","publisher","meta","timestamp","datelabel",
            "version","keywords","companies","persons","places",
            "self", "order", "counter", "aside", "assotiations",
            "list","listitem","text","link","image","quote",
            "form","video","audio","section","paragraph","table",
            "columns","column","tablerow","tablecell","videoplayer","audioplayer",
            "gallery", "content", "system", "buf", "build",
            "env", "var", "identifier", "desc", "descriptive", "headers", "tags",
            "null", "empty", "utc"
        };

        public TContext ParentContext { get; protected set; }
        public abstract void Read(TStringReader reader);
    }
}
