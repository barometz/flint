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
            var bundle = new PebbleBundle(ResourceManager.GetAppBundle(), new ZipImplementation());

            Assert.AreEqual(bundle.Manifest.Application.CRC, Crc32.Calculate(bundle.App));
        }

        [TestMethod]
        public void GeneratesCorrectChecksumForFirmware()
        {
            //PutBytesResponse Error: Error - 5/26/2014 4:00:46 PM: Calculated CRC is 0x7b0d1fe6 expected 0x32cebcd8, from 0x120000 for 0x26216 bytes, aborting... in put_bytes.c (290)

            var bundle = new PebbleBundle(ResourceManager.GetFirmwareBundle(), new ZipImplementation());

            Assert.AreEqual(bundle.Manifest.Firmware.CRC, Crc32.Calculate(bundle.Firmware));
        }
    }
}
