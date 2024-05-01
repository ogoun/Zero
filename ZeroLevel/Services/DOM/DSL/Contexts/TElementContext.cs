using DOM.DSL.Model;
using DOM.DSL.Services;
using DOM.DSL.Tokens;

namespace DOM.DSL.Contexts
{
    internal class TElementContext :
        TContext
    {
        private readonly string _name;
        private TToken _next;

        public TElementContext(TContext parent, string name)
        {
            ParentContext = parent;
            _name = name;
        }

        public override void Read(TStringReader reader)
        {
            if (reader.EOF == false && reader.Current == TChar.PropertyOrFuncStart)
            {
                if (reader.Move())
                {
                    reader.SkipBreaks();
                    var name = reader.ReadIdentity();
                    if (false == string.IsNullOrWhiteSpace(name))
                    {
                        reader.Move(name.Length);
                        if (this._name.Equals("system"))
                        {
                            // Like a '@system.ignorespace'
                            if (reader.Current == TChar.FuncArgsStart)
                            {
                                reader.Move();
                                reader.SkipBreaks();
                                var context = new TFunctionContext(this, name);
                                context.Read(reader);
                                _next = new TSystemToken { Command = name, Arg = context.Complete() };
                            }
                            else
                            {
                                _next = new TSystemToken { Command = name, Arg = null! };
                            }
                        }
                        else
                        {
                            if (reader.Current == TChar.FuncArgsStart)
                            {
                                // Function '@now.format(dd-mm)'
                                reader.Move();
                                var context = new TFunctionContext(this, name);
                                context.Read(reader);
                                _next = context.Complete();
                            }
                            else
                            {
                                // Property '@now.year'
                                var context = new TPropertyContext(this, name);
                                context.Read(reader);
                                _next = context.Complete();
                            }
                        }
                    }
                    else
                    {
                        _next = new TTextToken { Text = TChar.PropertyOrFuncStart + name };
                    }
                }
            }
        }

        public TToken Complete()
        {
            return new TElementToken { ElementName = _name, NextToken = _next?.Clone()! };
        }
    }
}