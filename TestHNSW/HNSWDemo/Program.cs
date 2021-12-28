using HNSWDemo.Tests;
using System;
using ZeroLevel.Services.Web;

namespace HNSWDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var uri = new Uri("https://hack33d.ru/bpla/upload.php?path=128111&get=0J/QuNC70LjQv9C10L3QutC+INCS0LvQsNC00LjQvNC40YAg0JzQuNGF0LDQudC70L7QstC40Yc7MDQuMDkuMTk1NCAoNjYg0LvQtdGCKTvQnNC+0YHQutC+0LLRgdC60LDRjzsxMjgxMTE7TEFfUkVaVVM7RkxZXzAy");
            var parts = UrlUtility.ParseQueryString(uri.Query);
            new AutoClusteringMNISTTest().Run();
            //new HistogramTest().Run();
            Console.WriteLine("Completed");
            Console.ReadKey();
        }
    }
}
