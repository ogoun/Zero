﻿using System;
using System.Net;
using ZeroLevel.Models;

namespace ZeroLevel.Services.Network
{
    public interface IExClient
    {
        event Action Connected;
        IPEndPoint Endpoint { get; }
        InvokeResult Send<T>(T obj);
        InvokeResult Send<T>(string inbox, T obj);
        InvokeResult Request<Treq, Tresp>(Treq obj, Action<Tresp> callback);
        InvokeResult Request<Treq, Tresp>(string inbox, Treq obj, Action<Tresp> callback);
        InvokeResult Request<Tresp>( Action<Tresp> callback);
        InvokeResult Request<Tresp>(string inbox, Action<Tresp> callback);
    }    
}
