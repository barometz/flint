using Flint.Core;
using Flint.Core.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Windows.Pebble.Tests.Zip
{
    [TestClass]
    public class ZipImplementationTests
    {
        [TestMethod]
        public void TestZipImplementation()
        {
            Dependencies.RegisterZipImplementation(() => new Pebble.Zip.Zip());
            PlatformDependencyTester.TestZipDependency();
        }
    }
}
