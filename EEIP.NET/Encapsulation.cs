using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sres.Net.EEIP
{
    public class Encapsulation
    {
        public CommandsEnum Command { get; set; }
        public ushort Length { get; set; }
        public uint SessionHandle { get; set; }
        public StatusEnum Status { get; set; }
        public byte[] SenderContext { get; set; } = new byte[8];
        private readonly uint options = 0;
        public List<byte> CommandSpecificData { get; set; } = new List<byte>();

        /// <summary>
        ///     Table 2-3.3 Error Codes
        /// </summary>
        public enum StatusEnum : uint
        {
            Success = 0x0000,
            InvalidCommand = 0x0001,
            InsufficientMemory = 0x0002,
            IncorrectData = 0x0003,
            InvalidSessionHandle = 0x0064,
            InvalidLength = 0x0065,
            UnsupportedEncapsulationProtocol = 0x0069
        }

        /// <summary>
        ///     Table 2-3.2 Encapsulation Commands
        /// </summary>
        public enum CommandsEnum : ushort
        {
            NOP = 0x0000,
            ListServices = 0x0004,
            ListIdentity = 0x0063,
            ListInterfaces = 0x0064,
            RegisterSession = 0x0065,
            UnRegisterSession = 0x0066,
            SendRRData = 0x006F,
            SendUnitData = 0x0070,
            IndicateStatus = 0x0072,
            Cancel = 0x0073
        }

        public byte[] SerializeToBytes()
        {
            var returnValue = new byte[24 + CommandSpecificData.Count];
            returnValue[0] = (byte)Command;
            returnValue[1] = (byte)((ushort)Command >> 8);
            returnValue[2] = (byte)Length;
            returnValue[3] = (byte)(Length >> 8);
            returnValue[4] = (byte)SessionHandle;
            returnValue[5] = (byte)(SessionHandle >> 8);
            returnValue[6] = (byte)(SessionHandle >> 16);
            returnValue[7] = (byte)(SessionHandle >> 24);
            returnValue[8] = (byte)Status;
            returnValue[9] = (byte)((ushort)Status >> 8);
            returnValue[10] = (byte)((ushort)Status >> 16);
            returnValue[11] = (byte)((ushort)Status >> 24);
            returnValue[12] = SenderContext[0];
            returnValue[13] = SenderContext[1];
            returnValue[14] = SenderContext[2];
            returnValue[15] = SenderContext[3];
            returnValue[16] = SenderContext[4];
            returnValue[17] = SenderContext[5];
            returnValue[18] = SenderContext[6];
            returnValue[19] = SenderContext[7];
            returnValue[20] = (byte)options;
            returnValue[21] = (byte)((ushort)options >> 8);
            returnValue[22] = (byte)((ushort)options >> 16);
            returnValue[23] = (byte)((ushort)options >> 24);
            for (var i = 0; i < CommandSpecificData.Count; i++)
                returnValue[24 + i] = CommandSpecificData[i];
            return returnValue;
        }


        /// <summary>
        ///     Table 2-4.4 CIP Identity Item
        /// </summary>
        public class CIPIdentityItem
        {
            public ushort ItemTypeCode { get; }                               //Code indicating item type of CIP Identity (0x0C)
            public ushort ItemLength { get; } //Number of bytes in item which follow (length varies depending on Product Name string)
            public ushort EncapsulationProtocolVersion { get; } //Encapsulation Protocol Version supported (also returned with Register Sesstion reply).
            public SocketAddress SocketAddress { get; } = new SocketAddress(); //Socket Address (see section 2-6.3.2)
            public ushort VendorID1 { get; } //Device manufacturers Vendor ID
            public ushort DeviceType1 { get; } //Device Type of product
            public ushort ProductCode1 { get; } //Product Code assigned with respect to device type
            public byte[] Revision1 { get; } = new byte[2];                    //Device revision
            public ushort Status1 { get; } //Current status of device
            public uint SerialNumber1 { get; } //Serial number of device
            public byte ProductNameLength { get; }
            public string ProductName1 { get; } //Human readable description of device
            public byte State1 { get; } //Current state of device

            public static CIPIdentityItem Deserialize(int startingByte, byte[] receivedData)
            {
                return new CIPIdentityItem(startingByte, receivedData);
            }

            private CIPIdentityItem(int startingByte, byte[] receivedData)
            {
                startingByte = startingByte + 2; //Skipped ItemCount
                ItemTypeCode = Convert.ToUInt16(receivedData[0 + startingByte]
                                                | (receivedData[1 + startingByte] << 8));
                ItemLength = Convert.ToUInt16(receivedData[2 + startingByte]
                                              | (receivedData[3 + startingByte] << 8));
                EncapsulationProtocolVersion = Convert.ToUInt16(receivedData[4 + startingByte]
                                                                | (receivedData[5 + startingByte] << 8));
                SocketAddress = SocketAddress.FromBytes(receivedData, 6 + startingByte);
                VendorID1 = Convert.ToUInt16(receivedData[22 + startingByte]
                                             | (receivedData[23 + startingByte] << 8));
                DeviceType1 = Convert.ToUInt16(receivedData[24 + startingByte]
                                               | (receivedData[25 + startingByte] << 8));
                ProductCode1 = Convert.ToUInt16(receivedData[26 + startingByte]
                                                | (receivedData[27 + startingByte] << 8));
                Revision1 = new[] {receivedData[28 + startingByte], receivedData[29 + startingByte]};
                Status1 = Convert.ToUInt16(receivedData[30 + startingByte]
                                           | (receivedData[31 + startingByte] << 8));
                SerialNumber1 = (uint)(receivedData[32 + startingByte]
                                       | (receivedData[33 + startingByte] << 8)
                                       | (receivedData[34 + startingByte] << 16)
                                       | (receivedData[35 + startingByte] << 24));
                ProductNameLength = receivedData[36 + startingByte];
                ProductName1 = Encoding.ASCII.GetString(receivedData, 37 + startingByte, ProductNameLength);
                State1 = receivedData[receivedData.Length - 1];
            }

            public CIPIdentityItem(SocketAddress socketAddress, 
                ushort vendorID1, ushort deviceType1, ushort productCode1, byte[] revision1, ushort status1, 
                uint serialNumber1, string productName1, byte state1,
                ushort encapsulationProtocolVersion = 1)
            {
                if (revision1 == null)
                    throw new ArgumentNullException(nameof(revision1));

                if (productName1 == null)
                    throw new ArgumentNullException(nameof(productName1));

                if (revision1.Length != 2)
                {
                    throw new ArgumentException("The size of the device revision array must be 2", nameof(revision1));
                }

                if (productName1.Length > 255)
                {
                    throw new ArgumentOutOfRangeException(nameof(productName1), "The product name must have a maximum length of 255");
                }

                ItemTypeCode = 0xC;
                EncapsulationProtocolVersion = encapsulationProtocolVersion;
                SocketAddress = socketAddress;
                VendorID1 = vendorID1;
                DeviceType1 = deviceType1;
                ProductCode1 = productCode1;
                Revision1 = revision1;
                Status1 = status1;
                SerialNumber1 = serialNumber1;
                ProductName1 = productName1;
                ProductNameLength = (byte)ProductName1.Length;
                State1 = state1;
                ItemLength = (ushort)(37 + ProductNameLength);
            }

            protected bool Equals(CIPIdentityItem other)
            {
                return ItemTypeCode == other.ItemTypeCode
                       && ItemLength == other.ItemLength
                       && EncapsulationProtocolVersion == other.EncapsulationProtocolVersion
                       && Equals(SocketAddress, other.SocketAddress)
                       && VendorID1 == other.VendorID1
                       && DeviceType1 == other.DeviceType1
                       && ProductCode1 == other.ProductCode1
                       && Revision1.SequenceEqual(other.Revision1)
                       && Status1 == other.Status1
                       && SerialNumber1 == other.SerialNumber1
                       && ProductNameLength == other.ProductNameLength
                       && ProductName1 == other.ProductName1
                       && State1 == other.State1;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != this.GetType())
                    return false;
                return Equals((CIPIdentityItem)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = ItemTypeCode.GetHashCode();
                    hashCode = (hashCode * 397) ^ ItemLength.GetHashCode();
                    hashCode = (hashCode * 397) ^ EncapsulationProtocolVersion.GetHashCode();
                    hashCode = (hashCode * 397) ^ SocketAddress.GetHashCode();
                    hashCode = (hashCode * 397) ^ VendorID1.GetHashCode();
                    hashCode = (hashCode * 397) ^ DeviceType1.GetHashCode();
                    hashCode = (hashCode * 397) ^ ProductCode1.GetHashCode();
                    hashCode = (hashCode * 397) ^ Revision1.GetHashCode();
                    hashCode = (hashCode * 397) ^ Status1.GetHashCode();
                    hashCode = (hashCode * 397) ^ (int)SerialNumber1;
                    hashCode = (hashCode * 397) ^ ProductNameLength.GetHashCode();
                    hashCode = (hashCode * 397) ^ (ProductName1 != null ? ProductName1.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ State1.GetHashCode();
                    return hashCode;
                }
            }
        }

        /// <summary>
        ///     Socket Address (see section 2-6.3.2)
        /// </summary>
        public class SocketAddress
        {
            public SocketAddress()
            {
            }

            /// <param name="sinAddress">Value of IP address. The value is in big-endian format</param>
            public SocketAddress(ushort sinFamily, ushort sinPort, uint sinAddress)
            {
                SIN_family = sinFamily;
                SIN_port = sinPort;
                SIN_Address = sinAddress;
            }

            public ushort SIN_family { get; }
            public ushort SIN_port { get; }
            
            /// <summary>
            /// Value of IP address. The value is in big-endian format
            /// </summary>
            public uint SIN_Address { get; }

            public byte[] SIN_Zero { get; } = new byte[8];

            protected bool Equals(SocketAddress other)
            {
                return SIN_family == other.SIN_family
                       && SIN_port == other.SIN_port
                       && SIN_Address == other.SIN_Address;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != this.GetType())
                    return false;
                return Equals((SocketAddress)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = SIN_family.GetHashCode();
                    hashCode = (hashCode * 397) ^ SIN_port.GetHashCode();
                    hashCode = (hashCode * 397) ^ (int)SIN_Address;
                    return hashCode;
                }
            }
            public static SocketAddress FromBytes(byte[] data, int startIndex)
            {
                var family = (ushort)(data[startIndex + 1]
                                      | (data[startIndex + 0] << 8));
                var port = (ushort)(data[startIndex + 3]
                                    | (data[startIndex + 2] << 8));
                var address = (uint)(data[startIndex + 4]
                                     | (data[startIndex + 5] << 8)
                                     | (data[startIndex + 6] << 16)
                                     | (data[startIndex + 7] << 24));

                return new SocketAddress(family, port, address);
            }
        }

        public class CommonPacketFormat
        {
            public ushort ItemCount { get; set; } = 2;
            public ushort AddressItem { get; set; } = 0x0000;
            public ushort AddressLength { get; set; } = 0;
            public ushort DataItem { get; set; } = 0xB2; //0xB2 = Unconnected Data Item
            public ushort DataLength { get; set; } = 8;
            public List<byte> Data { get; } = new List<byte>();
            public ushort SockaddrInfoItem_O_T { get; set; } = 0x8001; //8000 for O->T and 8001 for T->O - Volume 2 Table 2-6.9
            public ushort SockaddrInfoLength { get; set; } = 16;
            public SocketAddress SocketaddrInfo_O_T { get; set; } = null;

            public byte[] SerializeToBytes()
            {
                if (SocketaddrInfo_O_T != null)
                    ItemCount = 3;
                var returnValue = new byte[10 + Data.Count + (SocketaddrInfo_O_T == null ? 0 : 20)];
                returnValue[0] = (byte)ItemCount;
                returnValue[1] = (byte)(ItemCount >> 8);
                returnValue[2] = (byte)AddressItem;
                returnValue[3] = (byte)(AddressItem >> 8);
                returnValue[4] = (byte)AddressLength;
                returnValue[5] = (byte)(AddressLength >> 8);
                returnValue[6] = (byte)DataItem;
                returnValue[7] = (byte)(DataItem >> 8);
                returnValue[8] = (byte)DataLength;
                returnValue[9] = (byte)(DataLength >> 8);
                for (var i = 0; i < Data.Count; i++)
                    returnValue[10 + i] = Data[i];

                // Add Socket Address Info Item
                if (SocketaddrInfo_O_T != null)
                {
                    returnValue[10 + Data.Count + 0] = (byte)SockaddrInfoItem_O_T;
                    returnValue[10 + Data.Count + 1] = (byte)(SockaddrInfoItem_O_T >> 8);
                    returnValue[10 + Data.Count + 2] = (byte)SockaddrInfoLength;
                    returnValue[10 + Data.Count + 3] = (byte)(SockaddrInfoLength >> 8);
                    returnValue[10 + Data.Count + 5] = (byte)SocketaddrInfo_O_T.SIN_family;
                    returnValue[10 + Data.Count + 4] = (byte)(SocketaddrInfo_O_T.SIN_family >> 8);
                    returnValue[10 + Data.Count + 7] = (byte)SocketaddrInfo_O_T.SIN_port;
                    returnValue[10 + Data.Count + 6] = (byte)(SocketaddrInfo_O_T.SIN_port >> 8);
                    returnValue[10 + Data.Count + 8] = (byte)SocketaddrInfo_O_T.SIN_Address;
                    returnValue[10 + Data.Count + 9] = (byte)(SocketaddrInfo_O_T.SIN_Address >> 8);
                    returnValue[10 + Data.Count + 10] = (byte)(SocketaddrInfo_O_T.SIN_Address >> 16);
                    returnValue[10 + Data.Count + 11] = (byte)(SocketaddrInfo_O_T.SIN_Address >> 24);
                    returnValue[10 + Data.Count + 12] = SocketaddrInfo_O_T.SIN_Zero[0];
                    returnValue[10 + Data.Count + 13] = SocketaddrInfo_O_T.SIN_Zero[1];
                    returnValue[10 + Data.Count + 14] = SocketaddrInfo_O_T.SIN_Zero[2];
                    returnValue[10 + Data.Count + 15] = SocketaddrInfo_O_T.SIN_Zero[3];
                    returnValue[10 + Data.Count + 16] = SocketaddrInfo_O_T.SIN_Zero[4];
                    returnValue[10 + Data.Count + 17] = SocketaddrInfo_O_T.SIN_Zero[5];
                    returnValue[10 + Data.Count + 18] = SocketaddrInfo_O_T.SIN_Zero[6];
                    returnValue[10 + Data.Count + 19] = SocketaddrInfo_O_T.SIN_Zero[7];
                }

                return returnValue;
            }
        }
    }
}
