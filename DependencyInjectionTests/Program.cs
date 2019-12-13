using System;
using ZeroLevel;
using ZeroLevel.Patterns.DependencyInjection;
using ZeroLevel.Services.Reflection;

namespace DependencyInjectionTests
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


    class Program
    {
        static void Main(string[] args)
        {
            

            var config = Configuration.Create();
            config.Append("var", "bool isWorking");
            Injector.Default.Register<IDependencyContract, DependencyImplementation>();
            Injector.Default.Register<IMyContract, MyImplementation>();
            Injector.Default.Register<IConfiguration>(config);
            Injector.Default.Save<string>("delimeter", " = ");

            var instance = new MyImplementation();
            Injector.Default.Compose(instance);
            Console.WriteLine(instance.Run());
            Console.ReadKey();

            Injector.Dispose();
        }
    }
}
