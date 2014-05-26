using System;
using Flint.Core.Tests.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Flint.Core.Tests
{
    [TestClass]
    public class PebbleBundleTests
    {
        [TestMethod]
        public void ConstructorLoadsManifestForBundle()
        {
            Stream testBundle = ResourceManager.GetTestBundle();

            Dependencies.RegisterZipImplementation(() => new ZipImplementation());
            
            var bundle = new PebbleBundle(testBundle);
            
            var manifest = bundle.Manifest;
            Assert.IsNotNull(manifest);
            Assert.AreEqual(new DateTime(2013, 4, 13, 18, 3, 16), manifest.GeneratedAtDateTime);
            Assert.AreEqual("frontier", manifest.GeneratedBy);
            Assert.AreEqual(1, manifest.ManifestVersion);
            Assert.AreEqual("application", manifest.Type);
            Assert.AreEqual(PebbleBundle.BundleTypes.Application, bundle.BundleType);
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
    }
}
