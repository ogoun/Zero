using DOM.DSL.Model;
using DOM.DSL.Tokens;
using DOM.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel;
using ZeroLevel.DocumentObjectModel;

namespace DOM.DSL.Services
{
    internal class CustomBlocks
    {
        private readonly IDictionary<string, IEnumerable<TToken>> _blocks =
            new Dictionary<string, IEnumerable<TToken>>();

        public void Append(string name, IEnumerable<TToken> tokens)
        {
            if (false == _blocks.ContainsKey(name))
                _blocks.Add(name, tokens);
            else
            {
                _blocks[name] = null;
                _blocks[name] = tokens;
            }
        }

        public IEnumerable<TToken> Get(string name)
        {
            if (_blocks.ContainsKey(name))
                return _blocks[name];
            return Enumerable.Empty<TToken>();
        }

        public bool Exists(string name)
        {
            return _blocks.ContainsKey(name);
        }
    }

    internal class TRender
    {
        private readonly Document _document;
        private readonly TEnvironment _environment;
        private readonly DOMRenderElementCounter _counter;
        private readonly TRenderOptions _options = new TRenderOptions();
        private readonly TContainerFactory _factory;
        private readonly IDictionary<string, object> _buffer =
            new Dictionary<string, object>();

        private readonly CustomBlocks _blocks = new CustomBlocks();

        internal TContainerFactory Factory
        {
            get
            {
                return _factory;
            }
        }

        internal IDictionary<string, object> BufferDictionary
        {
            get
            {
                return _buffer;
            }
        }

        internal DOMRenderElementCounter Counter
        {
            get
            {
                return _counter;
            }
        }

        internal TRenderOptions Options
        {
            get
            {
                return _options;
            }
        }

        public TRender(Document document, TEnvironment environment)
        {
            _document = document;
            _environment = environment;
            _counter = new DOMRenderElementCounter();
            _factory = new TContainerFactory(this);
        }

        public void Resolve(TToken token, Action<TContainer> handler, bool release = true, TContainer self = null)
        {
            var self_copy = self == null ? null : _factory.Get(self.Current, self.Index);
            try
            {
                if (token is TTextToken)
                {
                    var container = _factory.Get(token.AsTextToken().Text);
                    handler(container);
                    if (release) _factory.Release(container);
                }
                else if (token is TElementToken)
                {
                    var containers = ResolveElementToken(token.AsElementToken(), self_copy);
                    foreach (var c in containers)
                    {
                        handler(c);
                        if (release) _factory.Release(c);
                    }
                }
                else if (token is TBlockToken)
                {
                    var containers = ResolveBlockToken(token.AsBlockToken(), self_copy);
                    foreach (var c in containers)
                    {
                        handler(c);
                        if (release) _factory.Release(c);
                    }
                }
            }
            finally
            {
                _factory.Release(self_copy);
            }
        }

        private TContainer[] ResolveElementToken(TElementToken token, TContainer self = null)
        {
            TContainer container = null;
            switch (token.ElementName.Trim().ToLowerInvariant())
            {
                // External
                case "now": container = _factory.Get(DateTime.Now); break;
                case "utc": container = _factory.Get(DateTime.Now.ToUniversalTime()); break;
                case "guid": container = _factory.Get(Guid.NewGuid()); break;
                case "nowutc": container = _factory.Get(DateTime.UtcNow); break;

                // Document
                case "id": container = _factory.Get(_document.Id); break;
                case "summary": container = _factory.Get(_document.Summary); break;
                case "header": container = _factory.Get(_document.Header); break;
                case "categories": container = _factory.Get(_document.Categories); break;
                case "directions":
                    container = _factory.Get(_document.Categories.
                            Select(c => c.DirectionCode).Distinct().ToList()); break;

                // Descriptive
                case "desc":
                case "descriptive": container = _factory.Get(_document.DescriptiveMetadata); break;
                case "author": container = _factory.Get(_document.DescriptiveMetadata.Byline); break;
                case "copyright": container = _factory.Get(_document.DescriptiveMetadata.CopyrightNotice); break;
                case "created": container = _factory.Get(_document.DescriptiveMetadata.Created); break;
                case "lang": container = _factory.Get(_document.DescriptiveMetadata.Language); break;
                case "priority": container = _factory.Get(_document.DescriptiveMetadata.Priority); break;
                case "source": container = _factory.Get(_document.DescriptiveMetadata.Source); break;
                case "publisher": container = _factory.Get(_document.DescriptiveMetadata.Publisher); break;
                case "meta":
                case "headers": container = _factory.Get(_document.DescriptiveMetadata.Headers); break;

                // Identifier
                case "identifier": container = _factory.Get(_document.Identifier); break;
                case "timestamp": container = _factory.Get(_document.Identifier.Timestamp); break;
                case "datelabel": container = _factory.Get(_document.Identifier.DateLabel); break;
                case "version": container = _factory.Get(_document.Identifier.Version); break;

                // Tags
                case "tags": container = _factory.Get(_document.TagMetadata); break;
                case "keywords": container = _factory.Get(_document.TagMetadata.Keywords); break;
                case "companies": container = _factory.Get(_document.TagMetadata.Companies); break;
                case "persons": container = _factory.Get(_document.TagMetadata.Persons); break;
                case "places": container = _factory.Get(_document.TagMetadata.Places); break;

                case "var": container = _factory.Get(_environment.CustomVariables); break;
                case "buf": container = _factory.Get(_buffer); break;
                case "env": container = _factory.Get(_environment); break;
                case "counter": container = _factory.Get(_counter); break;
                case "self": container = _factory.Get(self.Current, self.Index); break;
                case "content": container = _factory.Get(new TContentElement(_document)); break;
                case "aside": container = _factory.Get(_document.Aside); break;
                case "assotiations": container = _factory.Get(_document.Assotiations); break;
                case "null": container = _factory.Get(null); break;
                case "empty": container = _factory.Get(string.Empty); break;

                // Blocks
                case "build":
                    {
                        if (token.NextToken is TPropertyToken)
                        {
                            var block = new TBlockToken(_blocks.Get(token.NextToken.AsPropertyToken().PropertyName));
                            var result = ResolveBlockToken(block, self);
                            container = _factory.Get(result.Where(c => c.Current != null).Select(c => c.Current).ToList());
                            foreach (var c in result)
                                _factory.Release(c);
                        }
                        break;
                    }
            }

            if (container == null) container = _factory.Get(null);

            if (token.NextToken is TPropertyToken)
            {
                return new[] { ResolvePropertyToken(token.NextToken.AsPropertyToken(), container) };
            }
            else if (token.NextToken is TFunctionToken)
            {
                return new[] { ResolveFunctionToken(token.NextToken.AsFunctionToken(), container) };
            }
            else if (token.NextToken is TSystemToken)
            {
                ApplyRenderCommand(token.NextToken.AsSystemToken());
            }
            return new[] { container };
        }

        private TContainer ResolvePropertyToken(TPropertyToken token, TContainer container)
        {
            string property_index = null;
            Resolve(token.PropertyIndex, c => property_index = c.ToString());
            container.MoveToProperty(token.PropertyName, property_index);
            if (token.NextToken is TPropertyToken)
            {
                return ResolvePropertyToken(token.NextToken.AsPropertyToken(), container);
            }
            else if (token.NextToken is TFunctionToken)
            {
                return ResolveFunctionToken(token.NextToken.AsFunctionToken(), container);
            }
            return container;
        }

        private TContainer[] CalculateArguments(TFunctionToken token, TContainer self)
        {
            List<TContainer> args = new List<TContainer>();
            foreach (var a in token?.FunctionArgs ?? Enumerable.Empty<TToken>())
            {
                Resolve(a, c => args.Add(c), false, self);
            }
            return args.ToArray();
        }

        private TContainer ResolveFunctionToken(TFunctionToken token, TContainer container)
        {
            var args_calc = new Func<TContainer, TContainer[]>(self => CalculateArguments(token, self));
            container.ApplyFunction(token.FunctionName, args_calc);
            if (token.NextToken is TPropertyToken)
            {
                container = ResolvePropertyToken(token.NextToken.AsPropertyToken(), container);
            }
            else if (token.NextToken is TFunctionToken)
            {
                container = ResolveFunctionToken(token.NextToken.AsFunctionToken(), container);
            }
            return container;
        }

        private IEnumerable<TContainer> ResolveBlockToken(TBlockToken blockToken, TContainer self_parent = null)
        {
            switch (blockToken.Name)
            {
                case "block":
                    {
                        string name = null;
                        Resolve(blockToken.Condition, c => name = c.ToString(), true);
                        if (false == string.IsNullOrWhiteSpace(name))
                        {
                            _blocks.Append(name, blockToken.Body);
                            return Enumerable.Empty<TContainer>();
                        }
                    }
                    break;
                case "flow":
                    {
                        return new List<TContainer>
                        {
                            ResolveFlowBlockToken(blockToken.Body)
                        };
                    }
                case "if":
                    {
                        bool success = false;
                        Resolve(blockToken.Condition, c => success = c.As<bool>(), true, self_parent);
                        List<TContainer> result;
                        if (success)
                        {
                            var ls = self_parent == null ? null : _factory.Get(self_parent.Current, self_parent.Index);
                            result = ResolveSimpleBlockToken(blockToken, ls);
                            _factory.Release(ls);
                        }
                        else
                        {
                            result = new List<TContainer> { _factory.Get(null) };
                        }
                        return result;
                    }
                case "for":
                    {
                        var list = new List<TContainer>();
                        TContainer self_container = null;
                        Resolve(blockToken.Condition, c => self_container = c, false, self_parent);
                        if (self_container != null)
                        {
                            if (self_container.IsEnumerable)
                            {
                                int index = 0;
                                foreach (var t in (IEnumerable)self_container.Current)
                                {
                                    var self = _factory.Get(t, index);
                                    foreach (var bt in blockToken.Body)
                                    {
                                        Resolve(bt, c => list.Add(c), false, self);
                                    }
                                    _factory.Release(self);
                                    index++;
                                }
                            }
                            else
                            {
                                foreach (var bt in blockToken.Body)
                                {
                                    Resolve(bt, c => list.Add(c), false, self_container);
                                }
                            }
                        }
                        _factory.Release(self_container);
                        return list;
                    }
            }
            return ResolveSimpleBlockToken(blockToken, self_parent);
        }

        private List<TContainer> ResolveSimpleBlockToken(TBlockToken token, TContainer self = null)
        {
            var block = new List<TContainer>();
            foreach (var t in token.Body)
            {
                Resolve(t, c => block.Add(c), false, self);
            }
            return block;
        }

        private TContainer ResolveFlowBlockToken(IEnumerable<TToken> tokens)
        {
            var rules = new TFlowRules();
            rules.Bootstrap();
            foreach (var token in tokens)
            {
                if (token is TElementToken)
                {
                    var function = token.AsElementToken()?.NextToken?.AsFunctionToken();
                    var elementName = token.AsElementToken()?.ElementName;
                    var functionName = function?.FunctionName;
                    var rule_token = function?.FunctionArgs == null ?
                        null :
                        new TBlockToken(function.FunctionArgs.Select(a => a.Clone()));
                    string special = null;
                    if (functionName.Equals("special", StringComparison.OrdinalIgnoreCase))
                    {
                        var args = new List<TContainer>();
                        foreach (var a in function.FunctionArgs)
                        {
                            Resolve(a, c => args.Add(c), false);
                        }
                        special = string.Join(",", args.Select(a => a.ToString()));
                        foreach (var a in args)
                            _factory.Release(a);
                    }
                    rules.UpdateRule(elementName, functionName, rule_token, special);
                }
            }
            return _factory.Get(DocumentContentReader.ReadAs<string>(_document, new TContentToStringConverter(this, rules)));
        }

        private void ApplyRenderCommand(TSystemToken token)
        {
            List<TContainer> _args = new List<TContainer>();
            foreach (var a in token.Arg?.AsFunctionToken()?.FunctionArgs ?? Enumerable.Empty<TToken>())
            {
                Resolve(a, c => _args.Add(c), false);
            }
            var args = _args.ToArray();
            switch (token.Command)
            {
                case "log":
                    if (args?.Length > 0)
                    {
                        Log.Debug(args[0].ToString(), args.Skip(1).ToArray());
                    }
                    break;
                case "validate":
                    if (args?.Length == 1)
                    {
                        switch (args[0].ToString().Trim().ToLowerInvariant())
                        {
                            case "xml":
                                _options.ValidateAsXml = true;
                                break;
                            case "html":
                                _options.ValidateAsHtml = true;
                                break;
                            case "json":
                                _options.ValidateAsJson = true;
                                break;
                        }
                    }
                    break;
                case "fixwidth":
                    {
                        if (args?.Length == 1)
                        {
                            int width;
                            if (int.TryParse(args[0].ToString(), out width))
                            {
                                _options.MaxStringWidth = width;
                            }
                            else
                            {
                                _options.MaxStringWidth = -1;
                            }
                        }
                        else
                        {
                            _options.MaxStringWidth = -1;
                        }
                    }
                    break;
            }
            foreach (var a in args)
                _factory.Release(a);
        }
    }
}
