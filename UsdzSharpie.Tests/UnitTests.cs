using NUnit.Framework;
using System.IO;

namespace UsdzSharpie.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ReadReferenceUsdc()
        {
            var usdcReader = new UsdcReader();
            {
                var usdcPath = Path.Combine(Helper.ReferencePath, "reference.usdc");
                usdcReader.ReadUsdc(usdcPath);
            }
        }

        [Test]
        public void ReadExampleUsdz1()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample1);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz2()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample2);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz3()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample3);
                usdzReader.ReadUsdz(usdzPath);
            }
        }
        [Test]
        public void ReadExampleUsdz4()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample4);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz5()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample5);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz6()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample6);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz7()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample7);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz8()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample8);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz9()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample9);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz10()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample10);
                usdzReader.ReadUsdz(usdzPath);
            }
        }


        [Test]
        public void ReadExampleUsdz11()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample11);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz12()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample12);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz13()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample13);
                usdzReader.ReadUsdz(usdzPath);
            }
        }
        [Test]
        public void ReadExampleUsdz14()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample14);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz15()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample15);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz16()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample16);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz17()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample17);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz18()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample18);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz19()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample19);
                usdzReader.ReadUsdz(usdzPath);
            }
        }

        [Test]
        public void ReadExampleUsdz20()
        {
            var usdzReader = new UsdzReader();
            {
                var usdzPath = Path.Combine(Helper.ExamplesPath, Helper.UsdzExample20);
                usdzReader.ReadUsdz(usdzPath);
            }
        }
    }
}