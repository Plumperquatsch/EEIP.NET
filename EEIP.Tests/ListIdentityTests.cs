using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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
            var seed = TestHelper.CreateRandomSeed();
            var random = new Random(seed);

            var port = TestHelper.GetAvailablePort();
            Uri deviceUri = new UriBuilder("tcp", "localhost", port).Uri;
            using var eeipDevice = new EeipDeviceMock(deviceUri);

            ushort venderId = (ushort) random.Next();
            ushort deviceType = (ushort)random.Next();
            ushort productcode = (ushort)random.Next();
            byte[] deviceRevision = new byte[2] { (byte)random.Next(), (byte)random.Next() };
            ushort status = (ushort)random.Next();
            uint serialNumber = (uint)random.Next();
            string productName = "EEIP Device Mock";
            byte state = (byte)random.Next();
            uint deviceIp = (uint)Dns.GetHostAddresses(deviceUri.Host).First(x => x.AddressFamily == AddressFamily.InterNetwork).Address;

            var testIdentity = new Encapsulation.CIPIdentityItem(
                new Encapsulation.SocketAddress(2, (ushort)deviceUri.Port, deviceIp),
                venderId, deviceType, productcode, deviceRevision, status, serialNumber, productName, state);
            eeipDevice.Identity = testIdentity;

            using EEIPClient eeipClient = new Sres.Net.EEIP.EEIPClient();

            // Act
            Debug.WriteLine($"ListIdentityShallFindDeviceMock sends List Identity Request on port 0x{port.ToString("X")}");
            var identityList = await eeipClient.ListIdentityAsync((ushort)port);

            //Assert
            identityList.Should().NotBeNull();
            identityList.Any(identity => identity.ProductName1 == productName).Should().BeTrue();
            var matchingIdentity = identityList.Where(identity => identity.ProductName1 == productName).First();
            matchingIdentity.ItemLength.Should().Be(testIdentity.ItemLength);
            matchingIdentity.ItemTypeCode.Should().Be(testIdentity.ItemTypeCode);
            matchingIdentity.ProductCode1.Should().Be(testIdentity.ProductCode1);
            matchingIdentity.ProductName1.Should().Be(testIdentity.ProductName1);
            matchingIdentity.ProductNameLength.Should().Be(testIdentity.ProductNameLength);
            matchingIdentity.Revision1.Should().BeEquivalentTo(testIdentity.Revision1);
            matchingIdentity.SerialNumber1.Should().Be(testIdentity.SerialNumber1);
            matchingIdentity.State1.Should().Be(testIdentity.State1);
            matchingIdentity.Status1.Should().Be(testIdentity.Status1);
            matchingIdentity.VendorID1.Should().Be(testIdentity.VendorID1);
            matchingIdentity.SocketAddress.Should().Be(testIdentity.SocketAddress);
        }
    }
}
