using DOM.DSL.Model;
using DOM.DSL.Services;
using DOM.DSL.Tokens;
using System;
using System.Collections.Generic;

namespace DOM.DSL.Contexts
{
    internal class TBlockContext :
        TContext
    {
        private readonly string _name;
        private TToken _blockToken;
        private List<TToken> _tokens;

        public string Name { get { return _name; } }

        public TBlockContext(TContext parent, string name)
        {
            ParentContext = parent;
            _name = name;
            _tokens = new List<TToken>();
        }

        public override void Read(TStringReader reader)
        {
            if (_name.Equals("if", StringComparison.OrdinalIgnoreCase) ||
                    _name.Equals("for", StringComparison.OrdinalIgnoreCase))
            {
                var spaces_count = reader.SkipSpaces();
                if (reader.Current.Equals(TChar.TokenStart))
                {
                    reader.Move();
                    var name = reader.ReadIdentity();
                    if (name.Equals($"end{_name}"))
                    {
                        reader.Move(name.Length);
                        return;
                    }
                    if (_elementNames.Contains(name))
                    {
                        reader.Move(name.Length);
                        var elementContext = new TElementContext(this, name);
                        elementContext.Read(reader);
                        _blockToken = elementContext.Complete();
                        var body = new TRootContext(this);
                        body.Read(reader);
                        _tokens.AddRange(body.Complete());
                    }
                    else
                    {
                        _tokens.Add(new TTextToken { Text = "@" + _name + " @" + name });
                    }
                }
                else
                {
                    _tokens.Add(new TTextToken { Text = "@" + _name + new string(' ', spaces_count) });
                }
            }
            else if (_name.Equals("block", StringComparison.OrdinalIgnoreCase))
            {
                /*----------------------------------------------------------------*/
                if (reader.Current.Equals(TChar.PropertyOrFuncStart))
                {
                    reader.Move();
                    var name = reader.ReadIdentity();
                    if (name.Equals($"end{_name}"))
                    {
                        reader.Move(name.Length);
                        return;
                    }
                    if (name.Equals("to", StringComparison.OrdinalIgnoreCase))
                    {
                        reader.Move(name.Length);
                        if (reader.Current.Equals(TChar.FuncArgsStart))
                        {
                            reader.Move();
                            reader.SkipSpaces();
                            var var_name = reader.ReadIdentity();
                            if (string.IsNullOrWhiteSpace(var_name))
                            {
                                return;
                            }
                            reader.Move(var_name.Length);
                            reader.SkipSpaces();
                            if (reader.Current.Equals(TChar.FuncArgsEnd))
                            {
                                reader.Move();
                                reader.SkipSpaces();
                                reader.SkipBreaks();
                                _blockToken = new TTextToken { Text = var_name };
                                var body = new TRootContext(this);
                                body.Read(reader);
                                _tokens.AddRange(body.Complete());
                            }
                        }
                    }
                }
                /*----------------------------------------------------------------*/
            }
            else if (_name.Equals("comm", StringComparison.OrdinalIgnoreCase))
            {
                do
                {
                    var offset = reader.FindOffsetTo(TChar.TokenStart);
                    if (offset == -1) return;
                    reader.Move(offset + 1);
                    var name = reader.ReadIdentity();
                    if (name.Equals($"end{_name}"))
                    {
                        reader.Move(name.Length);
                        return;
                    }
                } while (reader.EOF == false);
            }
            else
            {
                var body = new TRootContext(this);
                body.Read(reader);
                _tokens.AddRange(body.Complete());
            }
        }

        public TToken Complete()
        {
            return new TBlockToken(_name, _blockToken, _tokens);
        }
    }
}