using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ZeroLevel.Services.Invokation;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Network.Services
{
    internal sealed class ExRouter
    {
        #region Routing
        private sealed class MRInvoker
        {
            /// <summary>
            /// Создает скомпилированное выражение для быстрого вызова метода, возвращает идентификатор выражения и делегат для вызова
            /// </summary>
            /// <param name="method">Оборачиваемый метод</param>
            /// <returns>Кортеж с идентификатором выражения и делегатом</returns>
            private static Invoker CreateCompiledExpression(MethodInfo method)
            {
                var targetArg = Expression.Parameter(typeof(object)); //  Цель на которой происходит вызов
                var argsArg = Expression.Parameter(typeof(object[])); //  Аргументы метода
                var parameters = method.GetParameters();
                Expression body = Expression.Call(
                    method.IsStatic
                        ? null
                        : Expression.Convert(targetArg, method.DeclaringType), //  тип в котором объявлен метод
                    method,
                    parameters.Select((p, i) =>
                        Expression.Convert(Expression.ArrayIndex(argsArg, Expression.Constant(i)), p.ParameterType)));
                if (body.Type == typeof(void))
                    body = Expression.Block(body, Expression.Constant(null));
                else if (body.Type.IsValueType)
                    body = Expression.Convert(body, typeof(object));
                return Expression.Lambda<Invoker>(body, targetArg, argsArg).Compile();
            }
            /// <summary>
            /// Оборачивает вызов делегата
            /// </summary>
            /// <param name="handler">Оборачиваемый делегат</param>
            /// <returns>Кортеж с идентификатором выражения и делегатом</returns>
            private static Invoker CreateCompiledExpression(Delegate handler)
            {
                return CreateCompiledExpression(handler.GetMethodInfo());
            }

            private object _instance;
            private Invoker _invoker;
            private Type _typeReq;
            private Type _typeResp;

            public static MRInvoker Create<T>(string inbox, Action<T, long, IZBackward> handler)
            {
                return new MRInvoker
                {
                    _typeReq = typeof(T),
                    _typeResp = null,
                    _instance = handler.Target,
                    _invoker = CreateCompiledExpression(handler)
                };
            }

            public static MRInvoker Create<Treq, Tresp>(string inbox, Func<Treq, long, IZBackward, Tresp> handler)
            {
                return new MRInvoker
                {
                    _typeReq = typeof(Treq),
                    _typeResp = typeof(Tresp),
                    _instance = handler.Target,
                    _invoker = CreateCompiledExpression(handler)
                };
            }

            public static MRInvoker Create<Tresp>(string inbox, Func<long, IZBackward, Tresp> handler)
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
                    this._invoker.Invoke(this._instance, new object[] { incoming, frame.FrameId, client });
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
        #endregion

        #region Registration
        public void RegisterInbox<T>(string inbox, Action<T, long, IZBackward> handler)
        {
            if (false == _handlers.ContainsKey(inbox))
            {
                _handlers.Add(inbox, new List<MRInvoker>());
            }
            _handlers[inbox].Add(MRInvoker.Create<T>(inbox, handler));
        }
        public void RegisterInbox<Treq, Tresp>(string inbox, Func<Treq, long, IZBackward, Tresp> hanlder)
        {
            if (false == _requestors.ContainsKey(inbox))
            {
                _requestors.Add(inbox, MRInvoker.Create<Treq, Tresp>(inbox, hanlder));
            }
            else
            {
                throw new Exception(string.Format("[SocketExchangeServer] Inbox {0} already exists", inbox));
            }
        }

        public void RegisterInbox<Tresp>(string inbox, Func<long, IZBackward, Tresp> hanlder)
        {
            if (false == _requestors.ContainsKey(inbox))
            {
                _requestors.Add(inbox, MRInvoker.Create<Tresp>(inbox, hanlder));
            }
            else
            {
                throw new Exception(string.Format("[SocketExchangeServer] Inbox {0} already exists", inbox));
            }
        }
        #endregion

        #region Invokation
        public void HandleMessage(Frame frame, IZBackward client)
        {
            try
            {
                if (_handlers.ContainsKey(frame.Inbox))
                {
                    foreach (var handler in _handlers[frame.Inbox])
                    {
                        try
                        {
                            handler.Invoke(frame, client);
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

        public Frame HandleRequest(Frame frame, IZBackward client)
        {
            try
            {
                if (_requestors.ContainsKey(frame.Inbox))
                {
                    return FrameBuilder.BuildResponseFrame(_requestors[frame.Inbox].Invoke(frame, client), frame);
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
        #endregion
    }
}
