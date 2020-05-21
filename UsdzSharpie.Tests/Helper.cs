using System;
using System.IO;

namespace UsdzSharpie.Tests
{
    public static class Helper
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
    }
}
