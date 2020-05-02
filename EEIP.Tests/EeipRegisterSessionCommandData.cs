using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Sres.Net.EEIP.Tests
{
    public class EeipRegisterSessionCommandData
    {
        public ushort EncapsulationProtocolVersion { get; }
        public ushort Options { get; }

        public EeipRegisterSessionCommandData(ushort encapsulationProtocolVersion = 1, ushort options = 0)
        {
            EncapsulationProtocolVersion = encapsulationProtocolVersion;
            Options = options;
        }

        public static EeipRegisterSessionCommandData Expand(Span<byte> encapsulationDataSpan)
        {
            if (encapsulationDataSpan.Length != 4)
            {
                throw new ArgumentOutOfRangeException("Command specific data of a Register Session packet must have a length of 4.");
            }
            Span<byte> protocolVersionSpan = encapsulationDataSpan.Slice(0, 2);
            Span<byte> optionsSpan = encapsulationDataSpan.Slice(2, 2);
            
            ushort protocolVersion = BinaryPrimitives.ReadUInt16LittleEndian(protocolVersionSpan);
            ushort options = BinaryPrimitives.ReadUInt16LittleEndian(optionsSpan);
            
            return new EeipRegisterSessionCommandData(protocolVersion, options);
        }

        public byte[] SerializeToBytes()
        {
            byte[] encapsulationData = new byte[4];
            Span<byte> encapsulationDataSpan = new Span<byte>(encapsulationData);
            
            Span<byte> protocolVersionSpan = encapsulationDataSpan.Slice(0, 2);
            Span<byte> optionsSpan = encapsulationDataSpan.Slice(2, 2);
            
            BinaryPrimitives.WriteUInt16LittleEndian(protocolVersionSpan, EncapsulationProtocolVersion);
            BinaryPrimitives.WriteUInt16LittleEndian(optionsSpan, Options);
            return encapsulationData;
        }
    }
}
