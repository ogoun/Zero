﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ZeroLevel.Network.SDL;
using ZeroLevel.Services.Invokation;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    public sealed class Router
        : IRouter
    {
        public event Action<ISocketClient> OnDisconnect = _ => { }; // must be never rised
        public event Action<IClient> OnConnect = _ => { }; // must be never rised

        #region Routing

        private sealed class MRInvoker
        {
            /// <summary>
            /// Creates a compiled expression for a quick method call, returns the identifier of the expression and a delegate for the call.
            /// </summary>
            private static Invoker CreateCompiledExpression(MethodInfo method)
            {
                var targetArg = Expression.Parameter(typeof(object)); //  Target
                var argsArg = Expression.Parameter(typeof(object[])); //  Method's args
                var parameters = method.GetParameters();
                Expression body = Expression.Call(
                    method.IsStatic
                        ? null
                        : Expression.Convert(targetArg, method.DeclaringType), //  Method's type
                    method,
                    parameters.Select((p, i) =>
                        Expression.Convert(Expression.ArrayIndex(argsArg, Expression.Constant(i)), p.ParameterType)));
                if (body.Type == typeof(void))
                    body = Expression.Block(body, Expression.Constant(null));
                else if (body.Type.IsValueType)
                    body = Expression.Convert(body, typeof(object));
                return Expression.Lambda<Invoker>(body, targetArg, argsArg).Compile();
            }

            private static Invoker CreateCompiledExpression(Delegate handler)
            {
                return CreateCompiledExpression(handler.GetMethodInfo());
            }

            private object _instance;
            private Invoker _invoker;
            private Type _typeReq;
            private Type _typeResp;
            private bool _noArguments = false;

            public static MRInvoker Create(MessageHandler handler)
            {
                return new MRInvoker
                {
                    _noArguments = true,
                    _typeReq = null!,
                    _typeResp = null!,
                    _instance = handler.Target,
                    _invoker = CreateCompiledExpression(handler)
                };
            }

            public static MRInvoker Create<T>(MessageHandler<T> handler)
            {
                return new MRInvoker
                {
                    _typeReq = typeof(T),
                    _typeResp = null!,
                    _instance = handler.Target,
                    _invoker = CreateCompiledExpression(handler)
                };
            }

            public static MRInvoker Create<Tresponse>(RequestHandler<Tresponse> handler)
            {
                return new MRInvoker
                {
                    _noArguments = true,
                    _typeReq = null!,
                    _typeResp = typeof(Tresponse),
                    _instance = handler.Target,
                    _invoker = CreateCompiledExpression(handler)
                };
            }

            public static MRInvoker Create<Trequest, Tresponse>(RequestHandler<Trequest, Tresponse> handler)
            {
                return new MRInvoker
                {
                    _typeReq = typeof(Trequest),
                    _typeResp = typeof(Tresponse),
                    _instance = handler.Target,
                    _invoker = CreateCompiledExpression(handler)
                };
            }

            public void Invoke(byte[] data, ISocketClient client)
            {
                if (_typeResp == null!)
                {
                    if (_noArguments)
                    {
                        this._invoker.Invoke(this._instance, new object[] { client });
                    }
                    else
                    {
                        var incoming = (_typeReq == typeof(byte[])) ? data : MessageSerializer.DeserializeCompatible(_typeReq, data);
                        this._invoker.Invoke(this._instance, new object[] { client, incoming });
                    }
                }
            }

            public void Invoke(byte[] data, ISocketClient client, Action<object> callback)
            {
                if (_typeReq == null!)
                {
                    callback(this._invoker.Invoke(this._instance, new object[] { client }));
                }
                else
                {
                    var incoming = (_typeReq == typeof(byte[])) ? data : MessageSerializer.DeserializeCompatible(_typeReq, data);
                    callback(this._invoker.Invoke(this._instance, new object[] { client, incoming }));
                }
            }

            public InboxServiceDescription GetDescription(string name)
            {
                return new InboxServiceDescription
                {
                    Name = name,
                    InboxKind = DetectKind(),
                    Target = _instance?.GetType()?.Name!,
                    IncomingType = GetIncomingTypeDescription(),
                    OutcomingType = GetOutcomingTypeDescription()
                };
            }

            private InboxType GetIncomingTypeDescription()
            {
                if (_typeReq == null!) return null!;
                return new InboxType
                {
                    Name = _typeReq.FullName,
                    Fields = _typeReq
                        .GetMembers(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance)
                        .Where(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property)
                        .Select(f => new KeyValuePair<string, string>(f.Name, (f.MemberType == MemberTypes.Property) ? (f as PropertyInfo)!.PropertyType.FullName : (f as FieldInfo)!.FieldType.FullName))
                        .ToDictionary(pair => pair.Key, pair => pair.Value)
                };
            }

            private InboxType GetOutcomingTypeDescription()
            {
                if (_typeResp == null!) return null!;
                return new InboxType
                {
                    Name = _typeResp.FullName,
                    Fields = _typeResp
                        .GetMembers(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance)
                        .Where(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property)
                        .Select(f => new KeyValuePair<string, string>(f.Name, (f.MemberType == MemberTypes.Property) ? (f as PropertyInfo)!.PropertyType.FullName : (f as FieldInfo)!.FieldType.FullName))
                        .ToDictionary(pair => pair.Key, pair => pair.Value)
                };
            }

            private InboxKind DetectKind()
            {
                if (_typeResp == null!)
                {
                    return _noArguments ? InboxKind.HandlerNoArgs : InboxKind.Handler;
                }
                return _noArguments ? InboxKind.ReqeustorNoArgs : InboxKind.Reqeustor;
            }
        }

        private readonly Dictionary<string, List<MRInvoker>> _handlers =
            new Dictionary<string, List<MRInvoker>>();

        private readonly Dictionary<string, MRInvoker> _requestors =
            new Dictionary<string, MRInvoker>();

        #endregion Routing

        #region Invokation

        public void HandleMessage(Frame frame, ISocketClient client)
        {
            try
            {
                List<MRInvoker> invokers;
                if (_handlers.TryGetValue(frame.Inbox, out invokers))
                {
                    foreach (var invoker in invokers)
                    {
                        try
                        {
                            invoker.Invoke(frame.Payload, client);
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[ExRouter] Fault handle incomind message");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[ExRouter] Fault handle incomind message");
            }
        }

        public void HandleRequest(Frame frame, ISocketClient client, int identity, Action<int, byte[]> handler)
        {
            try
            {
                MRInvoker invoker;
                if (_requestors.TryGetValue(frame.Inbox, out invoker))
                {
                    invoker.Invoke(frame.Payload, client
                        , result =>
                        {
                            handler(identity, MessageSerializer.SerializeCompatible(result));
                        });
                }
                else
                {
                    Log.SystemWarning($"[ExRouter] Not found inbox '{frame.Inbox}' for incoming request");
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[ExRouter] Fault handle incomind request");
            }
        }

        #endregion Invokation

        public bool ContainsInbox(string inbox)
        {
            return _handlers.ContainsKey(inbox) || _requestors.ContainsKey(inbox);
        }

        public bool ContainsHandlerInbox(string inbox)
        {
            return _handlers.ContainsKey(inbox);
        }

        public bool ContainsRequestorInbox(string inbox)
        {
            return _requestors.ContainsKey(inbox);
        }

        #region Message handlers registration
        public IServer RegisterInbox(string inbox, MessageHandler handler)
        {
            if (false == _handlers.ContainsKey(inbox))
            {
                _handlers.Add(inbox, new List<MRInvoker>());
            }
            _handlers[inbox].Add(MRInvoker.Create(handler));
            return this;
        }

        public IServer RegisterInbox<T>(string inbox, MessageHandler<T> handler)
        {
            if (false == _handlers.ContainsKey(inbox))
            {
                _handlers.Add(inbox, new List<MRInvoker>());
            }
            _handlers[inbox].Add(MRInvoker.Create<T>(handler));
            return this;
        }

        public IServer RegisterInbox(MessageHandler handler)
        {
            if (false == _handlers.ContainsKey(BaseSocket.DEFAULT_MESSAGE_INBOX))
            {
                _handlers.Add(BaseSocket.DEFAULT_MESSAGE_INBOX, new List<MRInvoker>());
            }
            _handlers[BaseSocket.DEFAULT_MESSAGE_INBOX].Add(MRInvoker.Create(handler));
            return this;
        }

        public IServer RegisterInbox<T>(MessageHandler<T> handler)
        {
            if (false == _handlers.ContainsKey(BaseSocket.DEFAULT_MESSAGE_INBOX))
            {
                _handlers.Add(BaseSocket.DEFAULT_MESSAGE_INBOX, new List<MRInvoker>());
            }
            _handlers[BaseSocket.DEFAULT_MESSAGE_INBOX].Add(MRInvoker.Create<T>(handler));
            return this;
        }

        public IServer RegisterInboxIfNoExists(string inbox, MessageHandler handler)
        {
            if (false == _handlers.ContainsKey(inbox))
            {
                _handlers.Add(inbox, new List<MRInvoker>());
                _handlers[inbox].Add(MRInvoker.Create(handler));
            }
            return this;
        }

        public IServer RegisterInboxIfNoExists<T>(string inbox, MessageHandler<T> handler)
        {
            if (false == _handlers.ContainsKey(inbox))
            {
                _handlers.Add(inbox, new List<MRInvoker>());
                _handlers[inbox].Add(MRInvoker.Create<T>(handler));
            }
            return this;
        }

        public IServer RegisterInboxIfNoExists(MessageHandler handler)
        {
            if (false == _handlers.ContainsKey(BaseSocket.DEFAULT_MESSAGE_INBOX))
            {
                _handlers.Add(BaseSocket.DEFAULT_MESSAGE_INBOX, new List<MRInvoker>());
                _handlers[BaseSocket.DEFAULT_MESSAGE_INBOX].Add(MRInvoker.Create(handler));
            }
            return this;
        }

        public IServer RegisterInboxIfNoExists<T>(MessageHandler<T> handler)
        {
            if (false == _handlers.ContainsKey(BaseSocket.DEFAULT_MESSAGE_INBOX))
            {
                _handlers.Add(BaseSocket.DEFAULT_MESSAGE_INBOX, new List<MRInvoker>());
                _handlers[BaseSocket.DEFAULT_MESSAGE_INBOX].Add(MRInvoker.Create<T>(handler));
            }
            return this;
        }
        #endregion 

        #region Request handlers registration
        public IServer RegisterInbox<Tresponse>(string inbox, RequestHandler<Tresponse> handler)
        {
            if (false == _requestors.ContainsKey(inbox))
            {
                _requestors.Add(inbox, MRInvoker.Create<Tresponse>(handler));
            }
            else
            {
                throw new Exception($"[SocketExchangeServer] Inbox {inbox} already exists");
            }
            return this;
        }

        public IServer RegisterInbox<Trequest, Tresponse>(string inbox, RequestHandler<Trequest, Tresponse> handler)
        {
            if (false == _requestors.ContainsKey(inbox))
            {
                _requestors.Add(inbox, MRInvoker.Create<Trequest, Tresponse>(handler));
            }
            else
            {
                throw new Exception($"[SocketExchangeServer] Inbox {inbox} already exists");
            }
            return this;
        }

        public IServer RegisterInbox<Tresponse>(RequestHandler<Tresponse> handler)
        {
            if (false == _requestors.ContainsKey(BaseSocket.DEFAULT_REQUEST_INBOX))
            {
                _requestors.Add(BaseSocket.DEFAULT_REQUEST_INBOX, MRInvoker.Create<Tresponse>(handler));
            }
            else
            {
                throw new Exception($"[SocketExchangeServer] Inbox {BaseSocket.DEFAULT_REQUEST_INBOX} already exists");
            }
            return this;
        }

        public IServer RegisterInbox<Trequest, Tresponse>(RequestHandler<Trequest, Tresponse> handler)
        {
            if (false == _requestors.ContainsKey(BaseSocket.DEFAULT_REQUEST_INBOX))
            {
                _requestors.Add(BaseSocket.DEFAULT_REQUEST_INBOX, MRInvoker.Create<Trequest, Tresponse>(handler));
            }
            else
            {
                throw new Exception($"[SocketExchangeServer] Inbox {BaseSocket.DEFAULT_REQUEST_INBOX} already exists");
            }
            return this;
        }
        #endregion

        public IEnumerable<InboxServiceDescription> CollectInboxInfo()
        {
            var inboxes = new List<InboxServiceDescription>();
            foreach (var handlers in _handlers)
            {
                foreach (var handler in handlers.Value)
                {
                    inboxes.Add(handler.GetDescription(handlers.Key));
                }
            }
            foreach (var requestor in _requestors)
            {
                inboxes.Add(requestor.Value.GetDescription(requestor.Key));
            }
            return inboxes;
        }
    }

    internal sealed class NullRouter
        : IRouter
    {
        public event Action<ISocketClient> OnDisconnect = _ => { };
        public event Action<IClient> OnConnect = _ => { };
        public void HandleMessage(Frame frame, ISocketClient client) { }
        public void HandleRequest(Frame frame, ISocketClient client, int identity, Action<int, byte[]> handler) { }
        public IServer RegisterInbox(string inbox, MessageHandler handler) { return this; }
        public IServer RegisterInbox<T>(string inbox, MessageHandler<T> handler) { return this; }
        public IServer RegisterInbox(MessageHandler handler) { return this; }
        public IServer RegisterInbox<T>(MessageHandler<T> handler) { return this; }
        public IServer RegisterInboxIfNoExists(string inbox, MessageHandler handler) { return this; }
        public IServer RegisterInboxIfNoExists<T>(string inbox, MessageHandler<T> handler) { return this; }
        public IServer RegisterInboxIfNoExists(MessageHandler handler) { return this; }
        public IServer RegisterInboxIfNoExists<T>(MessageHandler<T> handler) { return this; }
        public IServer RegisterInbox<Tresponse>(string inbox, RequestHandler<Tresponse> handler) { return this; }
        public IServer RegisterInbox<Trequest, Tresponse>(string inbox, RequestHandler<Trequest, Tresponse> handler) { return this; }
        public IServer RegisterInbox<Tresponse>(RequestHandler<Tresponse> handler) { return this; }
        public IServer RegisterInbox<Trequest, Tresponse>(RequestHandler<Trequest, Tresponse> handler) { return this; }
        public bool ContainsInbox(string inbox) => false;
        public bool ContainsHandlerInbox(string inbox) => false;
        public bool ContainsRequestorInbox(string inbox) => false;
        public IEnumerable<InboxServiceDescription> CollectInboxInfo() => Enumerable.Empty<InboxServiceDescription>();
    }
}
