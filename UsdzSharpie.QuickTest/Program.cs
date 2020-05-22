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
            //TestAll();

            //var usdcReader = new UsdcReader();
            //{
            //    var usdcPath = Path.Combine(Helper.ReferencePath, "reference.usdc");
            //    usdcReader.ReadUsdc(usdcPath);
            //}

            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample9);
                usdzReader.ReadUsdz(usdzPath);
            }
        }
    }
}
