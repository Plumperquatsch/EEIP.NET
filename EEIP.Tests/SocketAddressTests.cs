using FluentAssertions;
using NUnit.Framework;
using Sres.Net.EEIP;
using Sres.Net.EEIP.Tests.CIP;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using static Sres.Net.EEIP.Encapsulation;

namespace Sres.Net.EEIP.Tests
{
    [TestFixture()]
    public class SocketAddressTests
    {

        [Test()]
        public void SocketAddressEqualsTest()
        {
            // Arrange

            ushort sinFamily = 0x214;
            ushort sinPort = 0x123;
            uint sinAddress = 0x12345;
            
            var address1 = new SocketAddress(sinFamily, sinPort, sinAddress);
            var address2 = new SocketAddress(sinFamily, sinPort, sinAddress);

            // Act
            address1.Should().Be(address2);
        }


        [Test()]
        public void SocketAddressFromBytesTest()
        {
            // Arrange
            //var sockAddressBytes = new byte[8];
            //var sockAddressSpan = new Span<byte>(sockAddressBytes);
            //Span<byte> familySpan = sockAddressSpan.Slice(0, 2);
            //Span<byte> portSpan = sockAddressSpan.Slice(2, 2);
            //Span<byte> addressSpan = sockAddressSpan.Slice(4, 4);

            ushort family = 0x1234;
            ushort port = 0x5678;
            uint address = 0x1234567;
            var sockAddressBytes = new Encapsulation.SocketAddress(family, port, address).SerializeToBytes();

            //BinaryPrimitives.WriteUInt16BigEndian(familySpan, family);
            //BinaryPrimitives.WriteUInt16BigEndian(portSpan, port);
            //BinaryPrimitives.WriteUInt32BigEndian(addressSpan, address);

            // Act
            SocketAddress socketAddress = SocketAddress.FromBytes(sockAddressBytes, 0);

            // Assert
            socketAddress.SIN_family.Should().Be(family);
            socketAddress.SIN_port.Should().Be(port);
            socketAddress.SIN_Address.Should().Be(address);
        }
    }
}
