using System;
using System.IO;

namespace UsdzSharpie.Tests
{
    public static class Helper
    {
        public const string UsdzExample1 = "BeoSound_2.usdz";
        public const string UsdzExample2 = "chair_swan.usdz";
        public const string UsdzExample3 = "crua_hybrid.usdz";
        public const string UsdzExample4 = "CSC_Bag_Small.usdz";
        public const string UsdzExample5 = "cup_saucer_set.usdz";
        public const string UsdzExample6 = "fender_stratocaster.usdz";
        public const string UsdzExample7 = "flower_tulip.usdz";
        public const string UsdzExample8 = "gramophone.usdz";
        public const string UsdzExample9 = "Huracan-EVO-RWD-Spyder-opt-22.usdz";
        public const string UsdzExample10 = "pot_plant.usdz";
        public const string UsdzExample11 = "t51-helmet.usdz";
        public const string UsdzExample12 = "teapot.usdz";
        public const string UsdzExample13 = "toy_biplane.usdz";
        public const string UsdzExample14 = "toy_car.usdz";
        public const string UsdzExample15 = "toy_drummer.usdz";
        public const string UsdzExample16 = "toy_robot_vintage.usdz";
        public const string UsdzExample17 = "trowel.usdz";
        public const string UsdzExample18 = "tv_retro.usdz";
        public const string UsdzExample19 = "wateringcan.usdz";
        public const string UsdzExample20 = "wheelbarrow.usdz";

        public static string GetSolutionPath()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (!File.Exists(Path.Combine(path, "UsdzSharpie.sln")))
            {
                path = Path.GetFullPath(Path.Combine(path, ".."));
            }
            return path;
        }

        public static string SolutionPath => Helper.GetSolutionPath();

        public static string ExamplesPath => Path.Combine(SolutionPath, "Examples");

        public static string ReferencePath => Path.Combine(ExamplesPath, "Reference");
    }
}
