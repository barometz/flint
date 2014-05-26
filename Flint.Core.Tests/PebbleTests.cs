using System;
using System.Linq;
using System.Threading.Tasks;
using Flint.Core.Responses;
using Flint.Core.Tests.Responses;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Flint.Core.Tests
{
    [TestClass]
    public class PebbleTests
    {
        private const string TEST_PEBBLE_ID = "TESTID";

        [TestMethod]
        public void ConstructorTest()
        {
            var bluetoothConnection = new Mock<IBluetoothConnection>(MockBehavior.Strict);

            var pebble = new Pebble(bluetoothConnection.Object, TEST_PEBBLE_ID);

            Assert.AreEqual(TEST_PEBBLE_ID, pebble.PebbleID);
        }

        #region InstallFirmwareAsync Tests
        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public async Task InstallFirmwareAsyncRequiresBundle()
        {
            var bluetoothConnection = new Mock<IBluetoothConnection>();
            var pebble = new Pebble(bluetoothConnection.Object, TEST_PEBBLE_ID);

            await pebble.InstallFirmwareAsync(null);
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public async Task InstallFirmwareAsyncRequiresFirmwareBundle()
        {
            var bluetoothConnection = new Mock<IBluetoothConnection>();
            var pebble = new Pebble(bluetoothConnection.Object, TEST_PEBBLE_ID);

            var bundle = new Mock<PebbleBundle>();
            bundle.SetupGet(x => x.BundleType).Returns(PebbleBundle.BundleTypes.Application);

            await pebble.InstallFirmwareAsync(bundle.Object);
        }

        [TestMethod]
        public async Task InstallFirmwareAsyncTest()
        {
            var bundle = new Mock<PebbleBundle>();
            var firmwareBytes = new byte[16];
            var resourceBytes = new byte[4];
            bundle.SetupGet(x => x.BundleType).Returns(PebbleBundle.BundleTypes.Firmware);
            bundle.SetupGet(x => x.HasResources).Returns(true);
            bundle.SetupGet(x => x.Resources).Returns(resourceBytes);
            bundle.SetupGet(x => x.Firmware).Returns(firmwareBytes);

            var bluetoothConnection = new Mock<IBluetoothConnection>(MockBehavior.Strict);

            //Resource and firmware headers
            bluetoothConnection.Setup(x => x.Write(It.Is<byte[]>(b => b.Length == 11)))
                .Raises(x => x.DataReceived += null, bluetoothConnection, ResponseGenerator.GetBytesReceivedResponse(Endpoint.PutBytes))
                .Verifiable();
            //Resource data
            bluetoothConnection.Setup(x => x.Write(It.Is<byte[]>(b => b.Length == 13)))
                .Raises(x => x.DataReceived += null, bluetoothConnection, ResponseGenerator.GetBytesReceivedResponse(Endpoint.PutBytes))
                .Verifiable();
            //Firmware data
            bluetoothConnection.Setup(x => x.Write(It.Is<byte[]>(b => b.Length == 25)))
                .Raises(x => x.DataReceived += null, bluetoothConnection, ResponseGenerator.GetBytesReceivedResponse(Endpoint.PutBytes))
                .Verifiable();
            //Resource and Firmware CRC
            bluetoothConnection.Setup(x => x.Write(It.Is<byte[]>(b => b.Length == 9)))
                .Raises(x => x.DataReceived += null, bluetoothConnection, ResponseGenerator.GetBytesReceivedResponse(Endpoint.PutBytes))
                .Verifiable(); 
            //Put bytes complete message
            bluetoothConnection.Setup(x => x.Write(It.Is<byte[]>(b => b.Length == 5)))
                .Raises(x => x.DataReceived += null, bluetoothConnection, ResponseGenerator.GetBytesReceivedResponse(Endpoint.PutBytes))
                .Verifiable(); 

            var pebble = new Pebble(bluetoothConnection.Object, TEST_PEBBLE_ID);

            bool success = await pebble.InstallFirmwareAsync(bundle.Object);
            Assert.IsTrue(success);
            bluetoothConnection.Verify();
        }

        #endregion InstallFirmwareAsync Tests
    }
}
