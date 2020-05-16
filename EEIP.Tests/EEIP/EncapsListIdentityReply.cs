using Sres.Net.EEIP.Tests.CIP;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Sres.Net.EEIP.Tests
{
    public class EncapsListIdentityReply : Encapsulation
    {
        public ushort ProtocolVersion { get; } = 1;
        public ushort OptionsFlags { get; } = 0;

        public EncapsListIdentityReply(CIPIdentityItem identity, StatusEnum status = StatusEnum.Success, uint sessionHandlle = 0, byte[]? senderContext = null)
        {
            Command = CommandsEnum.ListIdentity;
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
            byte[] identityBytes = identity.SerializeToBytes();
            byte[] commandSpecificData = new byte[2 + identityBytes.Length];

            Span<byte> commandSpecificDataSpan = new Span<byte>(commandSpecificData);
            Span<byte> itemCountSpan = commandSpecificDataSpan.Slice(0, 2);
            BinaryPrimitives.WriteUInt16LittleEndian(itemCountSpan, 1);
            Span<byte> identitySpan = commandSpecificDataSpan.Slice(2, identityBytes.Length);
            identityBytes.CopyTo(identitySpan);

            CommandSpecificData = new List<byte>(commandSpecificData);
        }
    }
}
