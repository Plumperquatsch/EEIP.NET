using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sres.Net.EEIP.Tests
{
    [TestFixture]
    public class ListIdentityTests
    {
        [Test]
        public async Task ListIdentityShallFindDeviceMock()
        {
            // Arrange
            var port = TestHelper.GetAvailablePort();
            Uri deviceUri = new UriBuilder("tcp", "localhost", port).Uri;
            using var eeipDevice = new EeipDeviceMock(deviceUri);

            ushort venderId = 0x123;
            ushort deviceType = 0x234;
            ushort productcode = 0x345;
            byte[] deviceRevision = new byte[2] { 0x45, 0x67 };
            ushort status = 0x789;
            uint serialNumber = 0x1256;
            string productName = "EEIP Device Mock";
            byte state = 0x47;
            uint deviceIp = (uint)Dns.GetHostAddresses(deviceUri.Host).First(x => x.AddressFamily == AddressFamily.InterNetwork).Address;

            eeipDevice.Identity = new Encapsulation.CIPIdentityItem(
                new Encapsulation.SocketAddress(2, (ushort)deviceUri.Port, deviceIp),
                venderId, deviceType, productcode, deviceRevision, status, serialNumber, productName, state);

            using EEIPClient eeipClient = new Sres.Net.EEIP.EEIPClient();

            // Act
            Debug.WriteLine($"ListIdentityShallFindDeviceMock sends List Identity Request on port 0x{port.ToString("X")}");
            var identityList = await eeipClient.ListIdentityAsync((ushort)port);
            identityList.Should().NotBeNull();
            identityList.Where(identity => identity.SocketAddress.SIN_Address == deviceIp).Count().Should().Be(1);
        }
    }
}
