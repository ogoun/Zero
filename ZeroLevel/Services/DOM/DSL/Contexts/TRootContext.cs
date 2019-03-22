using DOM.DSL.Model;
using DOM.DSL.Services;
using DOM.DSL.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace DOM.DSL.Contexts
{
    internal class TRootContext :
        TContext
    {
        private readonly List<TToken> _tokens;

        public TRootContext()
        {
            ParentContext = null;
            _tokens = new List<TToken>();
        }

        public TRootContext(TContext parent)
        {
            ParentContext = parent;
            _tokens = new List<TToken>();
        }

        public override void Read(TStringReader reader)
        {
            var text = new StringBuilder();
            var flushTextToken = new Action(() =>
            {
                if (text.Length > 0)
                {
                    _tokens.Add(new TTextToken { Text = text.ToString() });
                    text.Clear();
                }
            });
            while (reader.EOF == false)
            {
                switch (reader.Current)
                {
                    #region Ecsaping
                    case TChar.Escape:
                        {
                            switch (reader.Next)
                            {
                                case 's':
                                    text.Append(' ');
                                    reader.Move(2);
                                    break;
                                case 'r':
                                    text.Append(TChar.CaretReturn);
                                    reader.Move(2);
                                    break;
                                case 'n':
                                    text.Append(TChar.Newline);
                                    reader.Move(2);
                                    break;
                                case 't':
                                    text.Append(TChar.Tab);
                                    reader.Move(2);
                                    break;
                                case '@':
                                case '(':
                                case ')':
                                case '.':
                                case ',':
                                case '\\':
                                    text.Append(reader.Next);
                                    reader.Move(2);
                                    break;
                                default:
                                    text.Append(reader.Current);
                                    reader.Move();
                                    break;
                            }
                        }
                        break;
                    #endregion

                    case TChar.TokenStart:
                        {
                            if (reader.Move())
                            {
                                var name = reader.ReadIdentity();

                                if (_elementNames.Contains(name))
                                {
                                    flushTextToken();
                                    reader.Move(name.Length);
                                    var elementContext = new TElementContext(this, name);
                                    reader.SkipBreaks();
                                    elementContext.Read(reader);
                                    _tokens.Add(elementContext.Complete());
                                }
                                else if (_blockNames.Contains(name))
                                {
                                    flushTextToken();
                                    reader.Move(name.Length);
                                    var blockContext = new TBlockContext(this, name);
                                    blockContext.Read(reader);
                                    _tokens.Add(blockContext.Complete());
                                }
                                else if (ParentContext != null && ParentContext is TBlockContext &&
                                    name.Equals("end" + (ParentContext as TBlockContext).Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    reader.Move(name.Length);
                                    flushTextToken();
                                    return;
                                }
                                else
                                {
                                    text.Append(TChar.TokenStart);
                                    text.Append(name);
                                    reader.Move(name.Length);
                                }
                            }
                            else
                            {
                                text.Append(TChar.TokenStart);
                                reader.Move();
                            }
                        }
                        break;
                    case TChar.CaretReturn:
                    case TChar.Newline:
                    case TChar.Tab:
                        reader.Move();
                        break;
                    default:
                        {
                            text.Append(reader.Current);
                            reader.Move();
                        }
                        break;
                }
            }
            flushTextToken();
        }

        public IEnumerable<TToken> Complete()
        {
            return _tokens;
        }
    }
}
