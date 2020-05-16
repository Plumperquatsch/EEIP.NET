using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Sres.Net.EEIP.Tests.EEIP
{
    public static class EEIPTestExtensions
    {
        private const int encapsulationSpecificDataOffset = 24;

        public static Encapsulation? ExpandEncapsulation(Span<byte> encapsulationSpan)
        {

            if (encapsulationSpan.Length < encapsulationSpecificDataOffset)
            {
                Debug.WriteLine($"The received EEIP packet should have at least {encapsulationSpecificDataOffset} bytes but had only {encapsulationSpan.Length} bytes.");
                return null;
            }
            var serializedCommand = BinaryPrimitives.ReadUInt16LittleEndian(encapsulationSpan.Slice(0, 2));
            if (!Enum.IsDefined(typeof(Encapsulation.CommandsEnum), serializedCommand))
            {
                Debug.WriteLine($"The received EEIP packet contains an undefined command {serializedCommand}");
                return null;
            }
            var serializedLength = BinaryPrimitives.ReadUInt16LittleEndian(encapsulationSpan.Slice(2, 2));
            if (encapsulationSpan.Length < encapsulationSpecificDataOffset + serializedLength)
            {
                Debug.WriteLine($"The received EEIP packet should have {encapsulationSpecificDataOffset + serializedLength} bytes but had only {encapsulationSpan.Length} bytes.");
                return null;
            }
            var serializedSessionHandle = BinaryPrimitives.ReadUInt32LittleEndian(encapsulationSpan.Slice(4, 4));
            var serializedStatus = BinaryPrimitives.ReadUInt32LittleEndian(encapsulationSpan.Slice(8, 4));
            if (!Enum.IsDefined(typeof(Encapsulation.StatusEnum), serializedStatus))
            {
                Debug.WriteLine($"The received EEIP packet contains an undefined status {serializedStatus}");
                return null;
            }
            var serializedSenderContext = encapsulationSpan.Slice(12, 8);
            var serializedOptions = BinaryPrimitives.ReadUInt32LittleEndian(encapsulationSpan.Slice(20, 4));

            var serializedSpecificData = encapsulationSpan.Slice(encapsulationSpecificDataOffset, serializedLength);

            Encapsulation encapsulation = new Encapsulation()
            {
                Command = (Encapsulation.CommandsEnum)serializedCommand,
                Length = serializedLength,
                SessionHandle = serializedSessionHandle,
                Status = (Encapsulation.StatusEnum)serializedStatus,
                SenderContext = serializedSenderContext.ToArray(),
                CommandSpecificData = new List<byte>(serializedSpecificData.ToArray())
            };

            return encapsulation;
        }

    }
}
