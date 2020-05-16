using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Sres.Net.EEIP.Tests.CIP
{
    public static class CipExtensions
    {
        public static byte[] SerializeToBytes(this Encapsulation.CIPIdentityItem identity)
        {
            byte[] identityBytes = new byte[identity.ItemLength];
            Span<byte> identitySpan = new Span<byte>(identityBytes);

            Span<byte> itemIdSpan = identitySpan.Slice(0, 2);
            Span<byte> itemLengthSpan = identitySpan.Slice(2, 2);
            Span<byte> encapsulationProtocolVersionSpan = identitySpan.Slice(4, 2);
            Span<byte> socketAddressSpan = identitySpan.Slice(6, 16);
            Span<byte> vendoerIdSpan = identitySpan.Slice(22, 2);
            Span<byte> deviceTypeSpan = identitySpan.Slice(24, 2);
            Span<byte> productCodeSpan = identitySpan.Slice(26, 2);
            Span<byte> revisionSpan = identitySpan.Slice(28, 2);
            Span<byte> statusSpan = identitySpan.Slice(30, 2);
            Span<byte> serialNumberSpan = identitySpan.Slice(32, 4);
            Span<byte> productNameSpan = identitySpan.Slice(36, identity.ProductNameLength);
            Span<byte> stateSpan = identitySpan.Slice(36 + identity.ProductNameLength, 1);

            BinaryPrimitives.WriteUInt16LittleEndian(itemIdSpan, identity.ItemTypeCode);
            BinaryPrimitives.WriteUInt16LittleEndian(itemLengthSpan, identity.ProductNameLength);
            BinaryPrimitives.WriteUInt16LittleEndian(encapsulationProtocolVersionSpan, identity.EncapsulationProtocolVersion);
            identity.SocketAddress.SerializeToBytes().CopyTo(socketAddressSpan);
            BinaryPrimitives.WriteUInt16LittleEndian(vendoerIdSpan, identity.VendorID1);
            BinaryPrimitives.WriteUInt16LittleEndian(deviceTypeSpan, identity.DeviceType1);
            BinaryPrimitives.WriteUInt16LittleEndian(productCodeSpan, identity.ProductCode1);
            identity.Revision1.CopyTo(revisionSpan);
            BinaryPrimitives.WriteUInt16LittleEndian(statusSpan, identity.Status1);
            BinaryPrimitives.WriteUInt32LittleEndian(serialNumberSpan, identity.SerialNumber1);
            Encoding.ASCII.GetBytes(identity.ProductName1).CopyTo(productNameSpan);
            stateSpan[0] = identity.State1;

            return identityBytes;
        }

        public static byte[] SerializeToBytes(this Encapsulation.SocketAddress socketAddress)
        {
            var sockAddressBytes = new byte[16];
            var sockAddressSpan = new Span<byte>(sockAddressBytes);
            Span<byte> familySpan = sockAddressSpan.Slice(0, 2);
            Span<byte> portSpan = sockAddressSpan.Slice(2, 2);
            Span<byte> addressSpan = sockAddressSpan.Slice(4, 4);
            Span<byte> sinZerorSpan = sockAddressSpan.Slice(8, 8);

            BinaryPrimitives.WriteUInt16BigEndian(familySpan, socketAddress.SIN_family);
            BinaryPrimitives.WriteUInt16BigEndian(portSpan, socketAddress.SIN_port);
            BinaryPrimitives.WriteUInt32BigEndian(addressSpan, socketAddress.SIN_Address);
            sinZerorSpan = socketAddress.SIN_Zero;

            return sockAddressBytes;
        }
    }
}
