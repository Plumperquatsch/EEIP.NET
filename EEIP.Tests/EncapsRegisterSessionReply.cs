using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Sres.Net.EEIP.Tests
{
    public class EncapsRegisterSessionReply : Encapsulation
    {
        public ushort ProtocolVersion { get; } = 1;
        public ushort OptionsFlags { get; } = 0;
        public EncapsRegisterSessionReply(StatusEnum status = StatusEnum.Success, uint sessionHandlle = 0, byte[]? senderContext = null)
        {
            Command = CommandsEnum.RegisterSession;
            SessionHandle = sessionHandlle;
            Status = status;
            SenderContext = new byte[8];
            if (senderContext != null)
            {
                if (senderContext.Length != 8)
                {
                    throw new EEIPTestException("The sender context must have a 8 bytes");
                }
                SenderContext = senderContext;
            }
            byte[] commandSpecificData = new byte[4];
            Span<byte> commandSpecificDataSpan = new Span<byte>(commandSpecificData);
            Span<byte> protocolVersionSpan = commandSpecificDataSpan.Slice(0, 2);
            BinaryPrimitives.WriteUInt16LittleEndian(protocolVersionSpan, ProtocolVersion);
            Span<byte> optionsFlagsSpan = commandSpecificDataSpan.Slice(2, 2);
            BinaryPrimitives.WriteUInt16LittleEndian(optionsFlagsSpan, OptionsFlags);
            CommandSpecificData = new List<byte>(commandSpecificData);
        }
    }
}
