using Flint.Core.Bundles;
using Flint.Core.Tests.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flint.Core.Tests
{
    [TestClass]
    public class Crc32Tests
    {
        [TestMethod]
        public void GeneratesCorrectChecksumForApp()
        {
            var bundle = new AppBundle();
            bundle.Load(ResourceManager.GetAppBundle(), new ZipImplementation());

            Assert.AreEqual(bundle.Manifest.Application.CRC, Crc32.Calculate(bundle.App));
        }

        [TestMethod]
        public void GeneratesCorrectChecksumForFirmware()
        {
            var bundle = new FirmwareBundle();
            bundle.Load(ResourceManager.GetFirmwareBundle(), new ZipImplementation());

            Assert.AreEqual(bundle.Manifest.Firmware.CRC, Crc32.Calculate(bundle.Firmware));
        }
    }
}
