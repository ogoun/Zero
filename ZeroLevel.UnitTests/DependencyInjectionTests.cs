using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using ZeroLevel.DependencyInjection;

namespace ZeroLevel.UnitTests
{
    public interface IDependencyContract
    {
        bool Invoke();
    }

    public class DependencyImplementation
        : IDependencyContract
    {
        public bool Invoke() => true;
    }

    public interface IMyContract
    {
        string Run();
    }

    public class MyImplementation
        : IMyContract
    {
        [Resolve]
        private IDependencyContract _dependency;
        [Parameter("delimeter")]
        private string _delimeter;
        [Resolve]
        private IConfiguration _config;

        public string Run() => $"{_config.First("var")}{_delimeter}{_dependency.Invoke()}";
    }

    public class DependencyInjectionTests
    {
        [Fact]
        public void ComposeTest()
        {
            // Arrange
            var config = Configuration.Create();
            config.Append("var", "bool isWorking");
            Injector.Default.Register<IDependencyContract, DependencyImplementation>();
            Injector.Default.Register<IMyContract, MyImplementation>();
            Injector.Default.Register<IConfiguration>(config);
            Injector.Default.Save<string>("delimeter", " = ");
            var instance = new MyImplementation();

            // Act
            Injector.Default.Compose(instance);

            // Assert
            var result = instance.Run();
            Assert.Equal("bool isWorking = True", result);

            Injector.Dispose();
        }
    }
}
