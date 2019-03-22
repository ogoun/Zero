using DOM.DSL.Model;
using DOM.DSL.Services;
using DOM.DSL.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DOM.DSL.Contexts
{
    internal class TPropertyContext :
        TContext
    {
        private string _name;
        private List<TToken> _indexTokens;
        private TToken _nextToken;

        public TPropertyContext(TContext parent, string name)
        {
            ParentContext = parent;
            _name = name;
            _indexTokens = new List<TToken>();
        }

        public override void Read(TStringReader reader)
        {
            if (reader.EOF) return;
            if (reader.Current == TChar.PropertyOrFuncStart)
            {
                if (reader.Move())
                {
                    reader.SkipBreaks();
                    var name = reader.ReadIdentity();
                    if (false == string.IsNullOrWhiteSpace(name))
                    {
                        reader.Move(name.Length);
                        if (reader.Current == TChar.FuncArgsStart)
                        {
                            // Function '@now.format(dd-mm)'
                            reader.Move();
                            var context = new TFunctionContext(this, name);
                            context.Read(reader);
                            _nextToken = context.Complete();
                        }
                        else
                        {
                            // Property '@now.year'
                            var context = new TPropertyContext(this, name);
                            context.Read(reader);
                            _nextToken = context.Complete();
                        }
                    }
                }
            }
            else if (reader.Current == TChar.PropertyIndexStart)
            {
                var text = new StringBuilder();
                var flushTextToken = new Action(() =>
                {
                    if (text.Length > 0)
                    {
                        _indexTokens.Add(new TTextToken { Text = text.ToString() });
                        text.Clear();
                    }
                });
                reader.Move();
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

                        case TChar.PropertyIndexEnd:
                            {
                                flushTextToken();
                                if (reader.Next == TChar.PropertyOrFuncStart)
                                {
                                    reader.Move(2);
                                    reader.SkipBreaks();
                                    var name = reader.ReadIdentity();
                                    reader.Move(name.Length);
                                    if (reader.Current.Equals(TChar.FuncArgsStart))
                                    {
                                        reader.Move();
                                        var context = new TFunctionContext(this, name);
                                        context.Read(reader);
                                        _nextToken = context.Complete();
                                    }
                                    else
                                    {
                                        text.Append(name);
                                    }
                                }
                                else
                                {
                                    reader.Move();
                                }
                            }
                            return;

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
                                        elementContext.Read(reader);
                                        _indexTokens.Add(elementContext.Complete());
                                    }
                                    else
                                    {
                                        text.Append(TChar.TokenStart);
                                        text.Append(reader.Current);
                                    }
                                }
                                else
                                {
                                    text.Append(reader.Current);
                                }
                            }
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
        }

        public TToken Complete()
        {
            return new TPropertyToken
            {
                PropertyName = _name,
                PropertyIndex = new TBlockToken(_name, null, _indexTokens.Select(t => t.Clone()).ToArray()),
                NextToken = _nextToken?.Clone()
            };
        }
    }
}
