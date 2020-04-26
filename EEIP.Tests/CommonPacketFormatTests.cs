using NUnit.Framework;
using Sres.Net.EEIP;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Buffers.Binary;
using FluentAssertions;

namespace Sres.Net.EEIP.Tests
{
    [TestFixture()]
    public class CommonPacketFormatTests
    {
        [Test()]
        public void SerializePacketAsUnconnectedDataWithSocketAddressTest()
        {

            // Arrange
            var seed = TestHelper.CreateRandomSeed();
            var random = new Random(seed);

            var dataCount = (byte)random.Next(0, 255);
            var testData = new byte[dataCount];
            random.NextBytes(testData);

            Encapsulation.SocketAddress sockAddress = new Encapsulation.SocketAddress(0x1234, 0x4567, 0x12345678);

            var commonPacket = new Encapsulation.CommonPacketFormat()
            {
                DataLength = dataCount,
                SocketaddrInfo_O_T = sockAddress
            };
            commonPacket.Data.AddRange(testData);

            // Act
            var commonPackteBytes = commonPacket.SerializeToBytes();

            //Assert
            Span<byte> commonPacketSpan = new Span<byte>(commonPackteBytes);
            var itemCount = BinaryPrimitives.ReadUInt16LittleEndian(commonPacketSpan.Slice(0, 2));
            var addressItem = BinaryPrimitives.ReadUInt16LittleEndian(commonPacketSpan.Slice(2, 2));
            var addressLength = BinaryPrimitives.ReadUInt16LittleEndian(commonPacketSpan.Slice(4, 2));
            
            var dataItem = BinaryPrimitives.ReadUInt16LittleEndian(commonPacketSpan.Slice(6, 2));
            var dataLength = BinaryPrimitives.ReadUInt16LittleEndian(commonPacketSpan.Slice(8, 2));
            var data = commonPacketSpan.Slice(10, dataCount);

            var sockaddrInfoItem = BinaryPrimitives.ReadUInt16LittleEndian(commonPacketSpan.Slice(10 + dataCount, 2));
            var sockaddrInfoLength = BinaryPrimitives.ReadUInt16LittleEndian(commonPacketSpan.Slice(10 + dataCount + 2, 2));
            var sinFamily = BinaryPrimitives.ReadUInt16BigEndian(commonPacketSpan.Slice(10 + dataCount + 4, 2));
            var sinPort = BinaryPrimitives.ReadUInt16BigEndian(commonPacketSpan.Slice(10 + dataCount + 6, 2));
            var sinAddr = BinaryPrimitives.ReadUInt32BigEndian(commonPacketSpan.Slice(10 + dataCount + 8, 4));
            var sinZero = commonPacketSpan.Slice(10 + dataCount + 12, 8);

            itemCount.Should().Be(commonPacket.ItemCount);
            addressItem.Should().Be(commonPacket.AddressItem);
            addressLength.Should().Be(commonPacket.AddressLength);

            dataItem.Should().Be(commonPacket.DataItem);
            dataLength.Should().Be(commonPacket.DataLength);
            data.ToArray().Should().BeEquivalentTo(testData);
            
            sockaddrInfoItem.Should().Be(commonPacket.SockaddrInfoItem_O_T);
            sockaddrInfoLength.Should().Be(commonPacket.SockaddrInfoLength);
            sinFamily.Should().Be(commonPacket.SocketaddrInfo_O_T.SIN_family);
            sinPort.Should().Be(commonPacket.SocketaddrInfo_O_T.SIN_port);
            sinAddr.Should().Be(commonPacket.SocketaddrInfo_O_T.SIN_Address);
            sinZero.ToArray().Should().BeEquivalentTo(commonPacket.SocketaddrInfo_O_T.SIN_Zero);
        }
    }
}
