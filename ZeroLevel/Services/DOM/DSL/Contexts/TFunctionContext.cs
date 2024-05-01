using DOM.DSL.Model;
using DOM.DSL.Services;
using DOM.DSL.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DOM.DSL.Contexts
{
    internal class TFunctionContext :
        TContext
    {
        private string _name;
        private List<TToken> _argTokens;
        private TToken _nextToken;

        public TFunctionContext(TContext parent, string name)
        {
            ParentContext = parent;
            _argTokens = new List<TToken>();
            _name = name;
        }

        public override void Read(TStringReader reader)
        {
            var text = new StringBuilder();
            var argTokens = new List<TToken>();
            var flushTextToken = new Action(() =>
            {
                if (text.Length > 0)
                {
                    argTokens.Add(new TTextToken { Text = text.ToString() });
                    text.Clear();
                }
            });
            var flushArgToken = new Action(() =>
            {
                if (argTokens.Count > 0)
                {
                    _argTokens.Add(new TBlockToken("", null!, argTokens.Select(t => t.Clone()).ToArray()));
                    argTokens.Clear();
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

                    #endregion Ecsaping

                    case TChar.FuncArgsEnd:
                        {
                            flushTextToken();
                            flushArgToken();
                            if (reader.Next == TChar.PropertyOrFuncStart)
                            {
                                if (reader.Move(2))
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
                            else
                            {
                                reader.Move();
                            }
                        }
                        return;

                    case TChar.FuncArgsSeparator:
                        flushTextToken();
                        flushArgToken();
                        reader.Move();
                        break;

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
                                    argTokens.Add(elementContext.Complete());
                                }
                                else if (_blockNames.Contains(name))
                                {
                                    flushTextToken();
                                    reader.Move(name.Length);
                                    var blockContext = new TBlockContext(this, name);
                                    blockContext.Read(reader);
                                    argTokens.Add(blockContext.Complete());
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
            flushArgToken();
        }

        public TToken Complete()
        {
            return new TFunctionToken
            {
                FunctionName = _name,
                NextToken = _nextToken?.Clone()!,
                FunctionArgs = _argTokens?.Select(t => t.Clone())!
            };
        }
    }
}