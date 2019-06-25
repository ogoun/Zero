using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ZeroLevel.Services.Invokation;

namespace ZeroLevel.Services._Network
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

            public static MRInvoker Create(Action<long, IZBackward> handler)
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

            public static MRInvoker Create<T>(Action<T, long, IZBackward> handler)
            {
                return new MRInvoker
                {
                    _typeReq = typeof(T),
                    _typeResp = null,
                    _instance = handler.Target,
                    _invoker = CreateCompiledExpression(handler)
                };
            }

            public static MRInvoker Create<Treq, Tresp>(Func<Treq, long, IZBackward, Tresp> handler)
            {
                return new MRInvoker
                {
                    _typeReq = typeof(Treq),
                    _typeResp = typeof(Tresp),
                    _instance = handler.Target,
                    _invoker = CreateCompiledExpression(handler)
                };
            }

            public static MRInvoker Create<Tresp>(Func<long, IZBackward, Tresp> handler)
            {
                return new MRInvoker
                {
                    _typeReq = null,
                    _typeResp = typeof(Tresp),
                    _instance = handler.Target,
                    _invoker = CreateCompiledExpression(handler)
                };
            }

            public object Invoke(Frame frame, IZBackward client)
            {
                if (_typeResp == null)
                {
                    var incoming = MessageSerializer.DeserializeCompatible(_typeReq, frame.Payload);
                    if (_noArguments)
                    {
                        this._invoker.Invoke(this._instance, new object[] { frame.FrameId, client });
                    }
                    else
                    {
                        this._invoker.Invoke(this._instance, new object[] { incoming, frame.FrameId, client });
                    }
                }
                else if (_typeReq == null)
                {
                    return this._invoker.Invoke(this._instance, new object[] { frame.FrameId, client });
                }
                else
                {
                    var incoming = MessageSerializer.DeserializeCompatible(_typeReq, frame.Payload);
                    return this._invoker.Invoke(this._instance, new object[] { incoming, frame.FrameId, client });
                }
                return null;
            }
        }

        private readonly Dictionary<string, List<MRInvoker>> _handlers =
            new Dictionary<string, List<MRInvoker>>();

        private readonly Dictionary<string, MRInvoker> _requestors =
            new Dictionary<string, MRInvoker>();

        #endregion Routing

        public void Incoming(FrameType type, byte[] data)
        {
            switch (type)
            {
                case FrameType.Message:
                    break;
                case FrameType.Request:
                    break;
                case FrameType.Response:
                    break;
            }
        }

        public void RegisterInbox(string inbox, MessageHandler handler)
        {
            throw new System.NotImplementedException();
        }

        public void RegisterInbox<T>(string inbox, MessageHandler<T> handler)
        {
            throw new System.NotImplementedException();
        }

        public void RegisterInbox(MessageHandler handler)
        {
            throw new System.NotImplementedException();
        }

        public void RegisterInbox<T>(MessageHandler<T> handler)
        {
            throw new System.NotImplementedException();
        }

        public void RegisterInbox<Tresponse>(string inbox, RequestHandler<Tresponse> handler)
        {
            throw new System.NotImplementedException();
        }

        public void RegisterInbox<Trequest, Tresponse>(string inbox, RequestHandler<Trequest, Tresponse> handler)
        {
            throw new System.NotImplementedException();
        }

        public void RegisterInbox<Tresponse>(RequestHandler<Tresponse> handler)
        {
            throw new System.NotImplementedException();
        }

        public void RegisterInbox<Trequest, Tresponse>(RequestHandler<Trequest, Tresponse> handler)
        {
            throw new System.NotImplementedException();
        }
    }
}
