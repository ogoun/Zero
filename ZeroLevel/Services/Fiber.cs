using System;
using System.Collections.Generic;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.Services
{
    public class Fiber
    {
        class Step
        {
            public Func<IEverythingStorage, IEverythingStorage> _handler;
            public Step _next;
        }

        Step _head = null;
        Step _tail = null;

        public Fiber()
        {
        }

        public Fiber Add(Func<IEverythingStorage, IEverythingStorage> action)
        {
            if (_head == null)
            {
                _head = _tail = new Step { _handler = action, _next = null };
            }
            else
            {
                var s = new Step { _handler = action, _next = null };
                _tail._next = s;
                _tail = s;
            }
            return this;
        }

        public IEnumerable<Func<IEverythingStorage, IEverythingStorage>> Iterate()
        {
            if (_head == null) yield break;
            var current = _head;
            while (current != null)
            {
                yield return current._handler;
                current = current._next;
            }
        }

        public IEverythingStorage Run(IEverythingStorage buffer = null)
        {
            var storage = buffer;
            foreach (var a in Iterate())
            {
                storage = a.Invoke(storage ?? new EverythingStorage());
            }
            return storage;
        }
    }
}
