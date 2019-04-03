using System.Collections.Generic;
using ZeroLevel.Patterns.DependencyInjection;

namespace ZeroLevel
{
    public static class Injector
    {
        private const string DEFAULT_CONTAINER_NAME = "__default_application_container__";
        private readonly static IContainerFactory _containerFactory;
        private readonly static IContainer _defaultContainer;

        public static IContainer Default
        {
            get
            {
                return _defaultContainer;
            }
        }

        static Injector()
        {
            _containerFactory = new ContainerFactory();
            _defaultContainer = _containerFactory.CreateContainer(DEFAULT_CONTAINER_NAME);
        }

        public static IEnumerable<string> ContainerNames
        {
            get
            {
                return _containerFactory.ContainerNames;
            }
        }

        public static IEnumerable<IContainer> Containers
        {
            get
            {
                return _containerFactory.Containers;
            }
        }

        public static bool Contains(string containerName)
        {
            return _containerFactory.Contains(containerName);
        }

        public static IContainer CreateContainer(string containerName)
        {
            return _containerFactory.CreateContainer(containerName);
        }

        public static void Dispose()
        {
            _containerFactory.Dispose();
        }

        public static IContainer GetContainer(string containerName)
        {
            return _containerFactory[containerName];
        }

        public static bool Remove(string containerName)
        {
            return _containerFactory.Remove(containerName);
        }
    }
}