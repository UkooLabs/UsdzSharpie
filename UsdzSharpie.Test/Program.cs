using System;
using System.IO;
using System.Reflection;

namespace UsdzSharpie.Test
{
    class Program
    {
        public static string GetSolutionPath()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (!File.Exists(Path.Combine(path, "UsdzSharpie.sln")))
            {
                path = Path.GetFullPath(Path.Combine(path, ".."));
            }
            return path;
        }

        static void Main(string[] args)
        {
            var usdcReader = new UsdcReader();
            {
                var usdcPath = Path.Combine(GetSolutionPath(), "example", "example.usdc");
                usdcReader.ReadUSDC(usdcPath);
            }
        }
    }
}
