using System;
using System.IO;
using UsdzSharpie.Tests;

namespace UsdzSharpie.QuickTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample9);
                usdzReader.ReadUsdz(usdzPath);
            }
        }
    }
}
