﻿using System;
using System.Net;

namespace ZeroLevel.Network
{
    public interface IExService
        : IDisposable
    {
        IPEndPoint Endpoint { get; }

        void RegisterInbox<T>(string inbox, Action<T, long, IZBackward> handler);

        void RegisterInbox<Treq, Tresp>(string inbox, Func<Treq, long, IZBackward, Tresp> handler);

        /// <summary>
        /// Replier without request
        /// </summary>
        void RegisterInbox<Tresp>(string inbox, Func<long, IZBackward, Tresp> handler);
    }
}