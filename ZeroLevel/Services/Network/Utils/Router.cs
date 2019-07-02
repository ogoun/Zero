using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ZeroLevel.Services.Invokation;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    public class Router
        : IRouter
    {
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
                    _typeReq = null,
                    _typeResp = null,
                    _instance = handler.Target,
                    _invoker = CreateCompiledExpression(handler)
                };
            }

            public static MRInvoker Create<T>(MessageHandler<T> handler)
            {
                return new MRInvoker
                {
                    _typeReq = typeof(T),
                    _typeResp = null,
                    _instance = handler.Target,
                    _invoker = CreateCompiledExpression(handler)
                };
            }

            public static MRInvoker Create<Tresponse>(RequestHandler<Tresponse> handler)
            {
                return new MRInvoker
                {
                    _noArguments = true,
                    _typeReq = null,
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

            public object Invoke(byte[] data, ISocketClient client)
            {
                if (_typeResp == null)
                {
                    var incoming = (_typeReq == typeof(byte[])) ? data : MessageSerializer.DeserializeCompatible(_typeReq, data);
                    if (_noArguments)
                    {
                        this._invoker.Invoke(this._instance, new object[] { client });
                    }
                    else
                    {
                        this._invoker.Invoke(this._instance, new object[] { client, incoming });
                    }
                }
                else if (_typeReq == null)
                {
                    return this._invoker.Invoke(this._instance, new object[] { client });
                }
                else
                {
                    var incoming = (_typeReq == typeof(byte[])) ? data : MessageSerializer.DeserializeCompatible(_typeReq, data);
                    return this._invoker.Invoke(this._instance, new object[] { client, incoming });
                }
                return null;
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
                if (_handlers.ContainsKey(frame.Inbox))
                {
                    foreach (var handler in _handlers[frame.Inbox])
                    {
                        try
                        {
                            handler.Invoke(frame.Payload, client);
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

        public byte[] HandleRequest(Frame frame, ISocketClient client)
        {
            try
            {
                if (_requestors.ContainsKey(frame.Inbox))
                {
                    return MessageSerializer.SerializeCompatible(_requestors[frame.Inbox].Invoke(frame.Payload, client));
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
            return null;
        }

        #endregion Invokation

        #region Message handlers registration
        public void RegisterInbox(string inbox, MessageHandler handler)
        {
            if (false == _handlers.ContainsKey(inbox))
            {
                _handlers.Add(inbox, new List<MRInvoker>());
            }
            _handlers[inbox].Add(MRInvoker.Create(handler));
        }

        public void RegisterInbox<T>(string inbox, MessageHandler<T> handler)
        {
            if (false == _handlers.ContainsKey(inbox))
            {
                _handlers.Add(inbox, new List<MRInvoker>());
            }
            _handlers[inbox].Add(MRInvoker.Create<T>(handler));
        }

        public void RegisterInbox(MessageHandler handler)
        {
            if (false == _handlers.ContainsKey(BaseSocket.DEFAULT_MESSAGE_INBOX))
            {
                _handlers.Add(BaseSocket.DEFAULT_MESSAGE_INBOX, new List<MRInvoker>());
            }
            _handlers[BaseSocket.DEFAULT_MESSAGE_INBOX].Add(MRInvoker.Create(handler));
        }

        public void RegisterInbox<T>(MessageHandler<T> handler)
        {
            if (false == _handlers.ContainsKey(BaseSocket.DEFAULT_MESSAGE_INBOX))
            {
                _handlers.Add(BaseSocket.DEFAULT_MESSAGE_INBOX, new List<MRInvoker>());
            }
            _handlers[BaseSocket.DEFAULT_MESSAGE_INBOX].Add(MRInvoker.Create<T>(handler));
        }
        #endregion 

        #region Request handlers registration
        public void RegisterInbox<Tresponse>(string inbox, RequestHandler<Tresponse> handler)
        {
            if (false == _requestors.ContainsKey(inbox))
            {
                _requestors.Add(inbox, MRInvoker.Create<Tresponse>(handler));
            }
            else
            {
                throw new Exception($"[SocketExchangeServer] Inbox {inbox} already exists");
            }
        }

        public void RegisterInbox<Trequest, Tresponse>(string inbox, RequestHandler<Trequest, Tresponse> handler)
        {
            if (false == _requestors.ContainsKey(inbox))
            {
                _requestors.Add(inbox, MRInvoker.Create<Trequest, Tresponse>(handler));
            }
            else
            {
                throw new Exception($"[SocketExchangeServer] Inbox {inbox} already exists");
            }
        }

        public void RegisterInbox<Tresponse>(RequestHandler<Tresponse> handler)
        {
            if (false == _requestors.ContainsKey(BaseSocket.DEFAULT_REQUEST_INBOX))
            {
                _requestors.Add(BaseSocket.DEFAULT_REQUEST_INBOX, MRInvoker.Create<Tresponse>(handler));
            }
            else
            {
                throw new Exception($"[SocketExchangeServer] Inbox {BaseSocket.DEFAULT_REQUEST_INBOX} already exists");
            }
        }

        public void RegisterInbox<Trequest, Tresponse>(RequestHandler<Trequest, Tresponse> handler)
        {
            if (false == _requestors.ContainsKey(BaseSocket.DEFAULT_REQUEST_INBOX))
            {
                _requestors.Add(BaseSocket.DEFAULT_REQUEST_INBOX, MRInvoker.Create<Trequest, Tresponse>(handler));
            }
            else
            {
                throw new Exception($"[SocketExchangeServer] Inbox {BaseSocket.DEFAULT_REQUEST_INBOX} already exists");
            }
        }
        #endregion
    }

    internal sealed class NullRouter
        : IRouter
    {
        public void HandleMessage(Frame frame, ISocketClient client) { }
        public byte[] HandleRequest(Frame frame, ISocketClient client) { return null; }
        public void RegisterInbox(string inbox, MessageHandler handler) { }
        public void RegisterInbox<T>(string inbox, MessageHandler<T> handler) { }
        public void RegisterInbox(MessageHandler handler) { }
        public void RegisterInbox<T>(MessageHandler<T> handler) { }
        public void RegisterInbox<Tresponse>(string inbox, RequestHandler<Tresponse> handler) { }
        public void RegisterInbox<Trequest, Tresponse>(string inbox, RequestHandler<Trequest, Tresponse> handler) { }
        public void RegisterInbox<Tresponse>(RequestHandler<Tresponse> handler) { }
        public void RegisterInbox<Trequest, Tresponse>(RequestHandler<Trequest, Tresponse> handler) { }
    }
}
