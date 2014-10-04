using System;
using Flint.Core.Bundles;
using Flint.Core.Tests.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Flint.Core.Tests
{
    [TestClass]
    public class PebbleBundleTests
    {
        [TestMethod]
        public void CanLoadInformationFromAppBundle()
        {
            Stream testBundle = ResourceManager.GetAppBundle();
            var bundle = new AppBundle();
            bundle.Load(testBundle, new ZipImplementation());
            
            var manifest = bundle.Manifest;
            Assert.IsNotNull(manifest);
            Assert.AreEqual(new DateTime(2013, 4, 13, 18, 3, 16), manifest.GeneratedAtDateTime);
            Assert.AreEqual("frontier", manifest.GeneratedBy);
            Assert.AreEqual(1, manifest.ManifestVersion);
            Assert.AreEqual("application", manifest.Type);
            Assert.IsTrue(manifest.Resources.Size > 0);
            Assert.IsTrue(bundle.HasResources);

            Assert.AreEqual(1, bundle.AppMetadata.AppMajorVersion);
            Assert.AreEqual(0, bundle.AppMetadata.AppMinorVersion);
            Assert.AreEqual("Shades", bundle.AppMetadata.AppName);
            Assert.AreEqual("1.0", bundle.AppMetadata.AppVersion);
            Assert.AreEqual("Barometz", bundle.AppMetadata.CompanyName);
            Assert.AreEqual((uint)1515157755, bundle.AppMetadata.CRC);
            Assert.AreEqual((uint)1, bundle.AppMetadata.Flags);
            Assert.AreEqual("PBLAPP", bundle.AppMetadata.Header);
            Assert.AreEqual((uint)0, bundle.AppMetadata.IconResourceID);
            Assert.AreEqual((uint)552, bundle.AppMetadata.Offset);
            Assert.AreEqual((uint)2, bundle.AppMetadata.RelocationListItemCount);
            Assert.AreEqual((uint)3860, bundle.AppMetadata.RelocationListStart);
            Assert.AreEqual(3, bundle.AppMetadata.SDKMajorVersion);
            Assert.AreEqual(1, bundle.AppMetadata.SDKMinorVersion);
            Assert.AreEqual("3.1", bundle.AppMetadata.SDKVersion);
            Assert.AreEqual(3860, bundle.AppMetadata.Size);
            Assert.AreEqual(8, bundle.AppMetadata.StructMajorVersion);
            Assert.AreEqual(1, bundle.AppMetadata.StructMinorVersion);
            Assert.AreEqual("8.1", bundle.AppMetadata.StructVersion);
            Assert.AreEqual((uint)2796, bundle.AppMetadata.SymbolTableAddress);
            Assert.AreEqual("ae9984f3-0404-409b-8a17-d50478c02d3e", bundle.AppMetadata.UUID.ToString());
        }

        [TestMethod]
        public void CanLoadInformationFromFirmwareBundle()
        {
            Stream testBundle = ResourceManager.GetFirmwareBundle();

            var bundle = new FirmwareBundle();
            bundle.Load(testBundle, new ZipImplementation());

            Assert.IsNotNull(bundle.Firmware);

            Assert.IsNotNull(bundle.Manifest);

            Assert.IsNotNull(bundle.Manifest.Firmware);
            Assert.AreEqual("tintin_fw.bin", bundle.Manifest.Firmware.Filename);
            Assert.AreEqual(new DateTime(2014, 5, 6, 6, 32, 23), bundle.Manifest.Firmware.TimestampDT);
            Assert.AreEqual(2824806042, bundle.Manifest.Firmware.CRC);
            Assert.AreEqual("v1_5", bundle.Manifest.Firmware.HardwareRevision);
            Assert.AreEqual("normal", bundle.Manifest.Firmware.Type);
            Assert.AreEqual(434731, bundle.Manifest.Firmware.Size);

            Assert.IsTrue(bundle.HasResources);
            Assert.IsNotNull(bundle.Resources);
        }
    }
}
