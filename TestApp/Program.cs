using System;

namespace TestApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs e)
        {
            if (e.Name.StartsWith("Microsoft.Build."))
            {
                // искать в локальной папке
            }
            return null;
        }
    }
}