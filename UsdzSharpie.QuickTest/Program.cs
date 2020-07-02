using System;
using System.IO;
using UsdzSharpie.Tests;

namespace UsdzSharpie.QuickTest
{
    class Program
    {
        private static void TestAll()
        {
            var usdzReader = new UsdzReader();
            {
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample1));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample2));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample3));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample4));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample5));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample6));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample7));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample8));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample9));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample10));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample11));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample12));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample13));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample14));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample15));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample16));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample17));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample18));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample19));
                usdzReader.ReadUsdz(Path.Combine(Helper.ExamplesPath, Helper.UsdzExample20));
            }
        }


        static void Main(string[] args)
        {
            var file = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample9);
            if (args.Length == 1)
            {
                file = args[0];
            }

            //TestAll();

            //var usdcReader = new UsdcReader();
            //{
            //    var usdcPath = Path.Combine(Helper.ReferencePath, "reference.usdc");
            //    usdcReader.ReadUsdc(usdcPath);
            //}

            //var usdcReader = new UsdcReader();
            //{
            //    var usdzPath = Path.Combine(Helper.ExamplesPath, "exampleevo.usdc");
            //    usdcReader.ReadUsdc(usdzPath);
            //}

            var logFile = Path.Combine(Helper.SolutionPath, "usdzsharpie.txt");
            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }

            Logger.LogFile = new StreamWriter(logFile);
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.GetFullPath(file);
                usdzReader.ReadUsdz(usdzPath);
            }
            Logger.LogFile.Close();

            //Console.WriteLine("Done, press Enter to close.");
            //Console.ReadLine();
        }
    }
}
