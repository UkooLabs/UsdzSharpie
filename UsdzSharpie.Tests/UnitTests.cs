using NUnit.Framework;
using System.IO;

namespace UsdzSharpie.Tests
{
    public class Tests
    {
        public string SolutionPath => Helper.GetSolutionPath();

        public string ExamplesPath => Path.Combine(SolutionPath, "Examples");

        public string ReferencePath => Path.Combine(ExamplesPath, "Reference");

        const string usdz1 = "BeoSound_2.usdz";
        const string usdz2 = "chair_swan.usdz";
        const string usdz3 = "crua_hybrid.usdz";
        const string usdz4 = "CSC_Bag_Small.usdz";
        const string usdz5 = "cup_saucer_set.usdz";
        const string usdz6 = "fender_stratocaster.usdz";
        const string usdz7 = "flower_tulip.usdz";
        const string usdz8 = "gramophone.usdz";
        const string usdz9 = "Huracan-EVO-RWD-Spyder-opt-22.usdz";
        const string usdz10 = "pot_plant.usdz";
        const string usdz11 = "t51-helmet.usdz";
        const string usdz12 = "teapot.usdz";
        const string usdz13 = "toy_biplane.usdz";
        const string usdz14 = "toy_car.usdz";
        const string usdz15 = "toy_drummer.usdz";
        const string usdz16 = "toy_robot_vintage.usdz";
        const string usdz17 = "trowel.usdz";
        const string usdz18 = "tv_retro.usdz";
        const string usdz19 = "wateringcan.usdz";
        const string usdz20 = "wheelbarrow.usdz";

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ReadReferenceUsdc()
        {
            var usdcReader = new UsdcReader();
            {
                var usdcPath = Path.Combine(ReferencePath, "reference.usdc");
                usdcReader.ReadUsdc(usdcPath);
            }
        }

        [Test]
        public void ReadExampleUsdz1()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz1);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz2()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz2);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz3()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz3);
                usdzReader.ReadUsdz(usdzPath);
            }
        }
        [Test]
        public void ReadExampleUsdz4()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz4);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz5()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz5);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz6()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz6);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz7()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz7);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz8()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz8);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz9()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz9);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz10()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz10);
                usdzReader.ReadUsdz(usdzPath);
            }
        }


        [Test]
        public void ReadExampleUsdz11()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz11);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz12()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz12);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz13()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz13);
                usdzReader.ReadUsdz(usdzPath);
            }
        }
        [Test]
        public void ReadExampleUsdz14()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz14);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz15()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz15);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz16()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz16);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz17()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz17);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz18()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz18);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz19()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz19);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz20()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(ExamplesPath, usdz20);
                usdzReader.ReadUsdz(usdzPath);
            }
        }
    }
}