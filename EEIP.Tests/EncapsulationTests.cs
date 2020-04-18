using FluentAssertions;
using NUnit.Framework;
using Sres.Net.EEIP;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Sres.Net.EEIP.Tests
{
    [TestFixture()]
    public class EncapsulationTests
    {
        [Test()]
        public void EncapsulationSerializeToBytesTest()
        {
            // Arrange
            var seed = TestHelper.CreateRandomSeed();
            var random = new Random(seed);

            UInt16 testDataLength = (UInt16)random.Next(1024);
            byte[] commandSpecificData = new byte[testDataLength];
            random.NextBytes(commandSpecificData);

            var testEncapsulation = new Encapsulation()
            {
                Command = (Encapsulation.CommandsEnum)random.Next(0xFFFF),
                Length = (UInt16)random.Next(1024),
                SessionHandle = (UInt16)random.Next(),
                Status = (Encapsulation.StatusEnum)random.Next(0xFFFF)
            };
            testEncapsulation.CommandSpecificData.AddRange(commandSpecificData);

            // Act
            byte[] serializedEncapsulation = testEncapsulation.SerializeToBytes();

            // Assert
            Span<byte> encapsulationSpan = new Span<byte>(serializedEncapsulation);
            var serializedCommand = BinaryPrimitives.ReadUInt16LittleEndian(encapsulationSpan.Slice(0, 2));
            var serializedLength = BinaryPrimitives.ReadUInt16LittleEndian(encapsulationSpan.Slice(2, 2));
            var serializedSessionHandle = BinaryPrimitives.ReadUInt32LittleEndian(encapsulationSpan.Slice(4, 4));
            var serializedStatus = BinaryPrimitives.ReadUInt32LittleEndian(encapsulationSpan.Slice(8, 4));
            var serializedSenderContext = encapsulationSpan.Slice(12, 8);
            var serializedOptions = BinaryPrimitives.ReadUInt32LittleEndian(encapsulationSpan.Slice(20, 4));
            var serializedSpecificData = encapsulationSpan.Slice(24, testDataLength);



            serializedCommand.Should().Be((UInt16)testEncapsulation.Command);
            serializedLength.Should().Be((UInt16)testEncapsulation.Length);
            serializedSessionHandle.Should().Be((UInt32)testEncapsulation.SessionHandle);
            serializedStatus.Should().Be((UInt32)testEncapsulation.Status);
            serializedSenderContext.ToArray().Should().BeEquivalentTo(new byte[8]); // Witin this ob the sender context can not be set because it is private
            serializedOptions.Should().Be(0); // Witin this ob options can not be set because it is private
            serializedSpecificData.ToArray().Should().BeEquivalentTo(commandSpecificData);
        }
    }
}
