using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flint.Core.Tests
{
    public static class PlatformDependencyTester
    {
        //Used to test various platform implementations of the flint dependencies
        public static void TestZipDependency()
        {
            var zip = Dependencies.GetZip();
            Assert.IsNotNull(zip);
            var zip2 = Dependencies.GetZip();
            Assert.IsNotNull(zip2);

            Assert.IsFalse(ReferenceEquals(zip, zip2));

            TestZipImplementation(zip);
        }

        private static void TestZipImplementation(IZip zip)
        {
            
        }
    }
}