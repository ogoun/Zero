using System;
using System.Collections.Generic;

namespace ZeroLevel.DependencyInjection
{
    public interface IContainerFactory : IDisposable
    {
        #region Properties

        IContainer this[string containerName] { get; }
        IEnumerable<string> ContainerNames { get; }
        IEnumerable<IContainer> Containers { get; }

        #endregion Properties

        #region Methods

        IContainer CreateContainer(string containerName);

        IContainer GetContainer(string containerName);

        bool Contains(string containerName);

        bool Remove(string containerName);

        #endregion Methods
    }
}