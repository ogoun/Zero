using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace ZeroLevel.Patterns.DependencyInjection
{
    public sealed class ContainerFactory : IContainerFactory
    {
        #region Private

        private bool _disposed = false;

        private static string GetKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                key = Guid.NewGuid().ToString();
            }
            return key.Trim().ToLower(CultureInfo.InvariantCulture);
        }

        private readonly ConcurrentDictionary<string, IContainer> _containers =
            new ConcurrentDictionary<string, IContainer>();

        #endregion Private

        #region Public

        public IContainer this[string containerName]
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ContainerFactory));
                return CreateContainer(containerName);
            }
        }

        public IEnumerable<string> ContainerNames
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ContainerFactory));
                return _containers.Keys;
            }
        }

        public IEnumerable<IContainer> Containers
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ContainerFactory));
                return _containers.Values;
            }
        }

        public bool Contains(string containerName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ContainerFactory));
            var key = GetKey(containerName);
            return _containers.ContainsKey(key);
        }

        public IContainer CreateContainer(string containerName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ContainerFactory));
            var key = GetKey(containerName);
            IContainer exists;
            if (_containers.TryGetValue(key, out exists))
            {
                return exists;
            }
            _containers.TryAdd(key, new Container());
            return _containers[key];
        }

        public IContainer GetContainer(string containerName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ContainerFactory));
            var key = GetKey(containerName);
            IContainer exists;
            if (_containers.TryGetValue(key, out exists))
            {
                return exists;
            }
            return null;
        }

        public bool Remove(string containerName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ContainerFactory));
            var key = GetKey(containerName);
            if (_containers.ContainsKey(key))
            {
                IContainer removed;
                if (_containers.TryRemove(key, out removed))
                {
                    removed.Dispose();
                    return true;
                }
            }
            return false;
        }

        #endregion Public

        #region IDisposable

        public void Dispose()
        {
            if (false == _disposed)
            {
                _disposed = true;
                foreach (var c in _containers.Values)
                {
                    c.Dispose();
                }
                _containers.Clear();
            }
        }

        #endregion IDisposable
    }
}