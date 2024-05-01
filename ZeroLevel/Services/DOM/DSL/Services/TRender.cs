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
        private readonly CustomBlocks _blocks = new CustomBlocks();
        internal TContainerFactory Factory { get; }
        internal IDictionary<string, object> BufferDictionary { get; } = new Dictionary<string, object>();
        internal DOMRenderElementCounter Counter { get; }
        internal TRenderOptions Options { get; } = new TRenderOptions();

        public TRender(Document document, TEnvironment environment)
        {
            _document = document;
            _environment = environment;
            Counter = new DOMRenderElementCounter();
            Factory = new TContainerFactory(this);
        }

        public void Resolve(TToken token, Action<TContainer> handler, bool release = true, TContainer self = null!)
        {
            var self_copy = self == null ? null : Factory.Get(self.Current, self.Index);
            try
            {
                if (token is TTextToken)
                {
                    var container = Factory.Get(token.AsTextToken().Text);
                    handler(container);
                    if (release) Factory.Release(container);
                }
                else if (token is TElementToken)
                {
                    var containers = ResolveElementToken(token.AsElementToken(), self_copy!);
                    foreach (var c in containers)
                    {
                        handler(c);
                        if (release) Factory.Release(c);
                    }
                }
                else if (token is TBlockToken)
                {
                    var containers = ResolveBlockToken(token.AsBlockToken(), self_copy!);
                    foreach (var c in containers)
                    {
                        handler(c);
                        if (release) Factory.Release(c);
                    }
                }
            }
            finally
            {
                Factory.Release(self_copy!);
            }
        }

        private TContainer[] ResolveElementToken(TElementToken token, TContainer self = null!)
        {
            TContainer container = null!;
            switch (token.ElementName.Trim().ToLowerInvariant())
            {
                // External
                case "now": container = Factory.Get(DateTime.Now); break;
                case "utc": container = Factory.Get(DateTime.Now.ToUniversalTime()); break;
                case "guid": container = Factory.Get(Guid.NewGuid()); break;
                case "nowutc": container = Factory.Get(DateTime.UtcNow); break;

                // Document
                case "id": container = Factory.Get(_document.Id); break;
                case "summary": container = Factory.Get(_document.Summary); break;
                case "header": container = Factory.Get(_document.Header); break;
                case "categories": container = Factory.Get(_document.Categories); break;
                case "directions":
                    container = Factory.Get(_document.Categories.
                            Select(c => c.DirectionCode).Distinct().ToList()); break;

                // Descriptive
                case "desc":
                case "descriptive": container = Factory.Get(_document.DescriptiveMetadata); break;
                case "author": container = Factory.Get(_document.DescriptiveMetadata.Byline); break;
                case "copyright": container = Factory.Get(_document.DescriptiveMetadata.CopyrightNotice); break;
                case "created": container = Factory.Get(_document.DescriptiveMetadata.Created); break;
                case "lang": container = Factory.Get(_document.DescriptiveMetadata.Language); break;
                case "priority": container = Factory.Get(_document.DescriptiveMetadata.Priority); break;
                case "source": container = Factory.Get(_document.DescriptiveMetadata.Source); break;
                case "publisher": container = Factory.Get(_document.DescriptiveMetadata.Publisher); break;
                case "meta":
                case "headers": container = Factory.Get(_document.DescriptiveMetadata.Headers); break;

                // Identifier
                case "identifier": container = Factory.Get(_document.Identifier); break;
                case "timestamp": container = Factory.Get(_document.Identifier.Timestamp); break;
                case "datelabel": container = Factory.Get(_document.Identifier.DateLabel); break;
                case "version": container = Factory.Get(_document.Identifier.Version); break;

                // Tags
                case "tags": container = Factory.Get(_document.TagMetadata); break;
                case "keywords": container = Factory.Get(_document.TagMetadata.Keywords); break;
                case "companies": container = Factory.Get(_document.TagMetadata.Companies); break;
                case "persons": container = Factory.Get(_document.TagMetadata.Persons); break;
                case "places": container = Factory.Get(_document.TagMetadata.Places); break;

                case "var": container = Factory.Get(_environment.CustomVariables); break;
                case "buf": container = Factory.Get(BufferDictionary); break;
                case "env": container = Factory.Get(_environment); break;
                case "counter": container = Factory.Get(Counter); break;
                case "self": container = Factory.Get(self.Current, self.Index); break;
                case "content": container = Factory.Get(new TContentElement(_document)); break;
                case "aside": container = Factory.Get(_document.Attachments); break;
                case "assotiations": container = Factory.Get(_document.Assotiations); break;
                case "null": container = Factory.Get(null!); break;
                case "empty": container = Factory.Get(string.Empty); break;

                // Blocks
                case "build":
                    {
                        if (token.NextToken is TPropertyToken)
                        {
                            var block = new TBlockToken(_blocks.Get(token.NextToken.AsPropertyToken().PropertyName));
                            var result = ResolveBlockToken(block, self);
                            container = Factory.Get(result.Where(c => c.Current != null).Select(c => c.Current).ToList());
                            foreach (var c in result)
                                Factory.Release(c);
                        }
                        break;
                    }
            }

            if (container == null) container = Factory.Get(null!);

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
            string property_index = null!;
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

        private IEnumerable<TContainer> ResolveBlockToken(TBlockToken blockToken, TContainer self_parent = null!)
        {
            switch (blockToken.Name)
            {
                case "block":
                    {
                        string name = null!;
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
                            var ls = self_parent == null ? null : Factory.Get(self_parent.Current, self_parent.Index);
                            result = ResolveSimpleBlockToken(blockToken, ls!);
                            Factory.Release(ls);
                        }
                        else
                        {
                            result = new List<TContainer> { Factory.Get(null!) };
                        }
                        return result;
                    }
                case "for":
                    {
                        var list = new List<TContainer>();
                        TContainer self_container = null!;
                        Resolve(blockToken.Condition, c => self_container = c, false, self_parent);
                        if (self_container != null)
                        {
                            if (self_container.IsEnumerable)
                            {
                                int index = 0;
                                foreach (var t in (IEnumerable)self_container.Current)
                                {
                                    var self = Factory.Get(t, index);
                                    foreach (var bt in blockToken.Body)
                                    {
                                        Resolve(bt, c => list.Add(c), false, self);
                                    }
                                    Factory.Release(self);
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
                        Factory.Release(self_container!);
                        return list;
                    }
            }
            return ResolveSimpleBlockToken(blockToken, self_parent);
        }

        private List<TContainer> ResolveSimpleBlockToken(TBlockToken token, TContainer self = null!)
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
                    if (elementName != null)
                    {
                        var functionName = function?.FunctionName ?? string.Empty;
                        var rule_token = function?.FunctionArgs == null ?
                            null :
                            new TBlockToken(function.FunctionArgs.Select(a => a.Clone()));
                        string special = null!;
                        if (functionName.Equals("special", StringComparison.OrdinalIgnoreCase))
                        {
                            var args = new List<TContainer>();
                            foreach (var a in function?.FunctionArgs ?? Enumerable.Empty<TToken>())
                            {
                                Resolve(a, c => args.Add(c), false);
                            }
                            special = string.Join(",", args.Select(a => a.ToString()));
                            foreach (var a in args)
                                Factory.Release(a);
                        }
                        rules.UpdateRule(elementName, functionName, rule_token!, special);
                    }
                }
            }
            return Factory.Get(DocumentContentReader.ReadAs<string>(_document, new TContentToStringConverter(this, rules)));
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
                                Options.ValidateAsXml = true;
                                break;

                            case "html":
                                Options.ValidateAsHtml = true;
                                break;

                            case "json":
                                Options.ValidateAsJson = true;
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
                                Options.MaxStringWidth = width;
                            }
                            else
                            {
                                Options.MaxStringWidth = -1;
                            }
                        }
                        else
                        {
                            Options.MaxStringWidth = -1;
                        }
                    }
                    break;
            }
            foreach (var a in args!)
            {
                Factory.Release(a);
            }
        }
    }
}