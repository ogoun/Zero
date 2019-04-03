using DOM.DSL.Contexts;
using DOM.DSL.Model;
using DOM.DSL.Services;
using DOM.DSL.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroLevel;
using ZeroLevel.DocumentObjectModel;

namespace DOM.DSL
{
    public static class TEngine
    {
        public static IEnumerable<TToken> Parse(string template)
        {
            if (false == string.IsNullOrEmpty(template))
            {
                IEnumerable<TToken> tokens;
                try
                {
                    var reader = new TStringReader(template);
                    var context = new TRootContext();
                    context.Read(reader);
                    tokens = context.Complete();
                    return tokens;
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"Fault parse template '{template}' to token list");
                }
            }
            return Enumerable.Empty<TToken>();
        }

        public static string Apply(Document document, IEnumerable<TToken> tokens, TEnvironment environment, bool ignore_fault = true)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }
            var text = new StringBuilder();
            try
            {
                var resolver = new TRender(document, environment);
                foreach (var token in tokens)
                {
                    resolver.Resolve(token, c =>
                    {
                        var line = c.ToString();
                        if (string.IsNullOrEmpty(line) == false)
                        {
                            if (resolver.Options.MaxStringWidth > 0)
                            {
                                text.Append(TRenderUtils.SplitOn(line, resolver.Options.MaxStringWidth));
                            }
                            else
                            {
                                text.Append(line);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"Fault transform document '{document.Id} by token list'. {ex.ToString()}");
                if (ignore_fault == false)
                {
                    throw ex;
                }
            }
            return text.ToString();
        }
    }
}