using Sres.Net.EEIP.Tests.CIP;
using Sres.Net.EEIP.Tests.EEIP;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sres.Net.EEIP.Tests
{
    public class EeipDeviceMock : IDisposable
    {
        public ushort EncapsulationProtocolVersion { get; } = 1;
        public bool Connected { get; private set; }
        public bool SessionRegistered { get; private set; }
        public uint SessionHandle { get; private set; }

        public IPAddress DeviceIp { get; private set; }
        public int EeipPort { get; private set; }

        public Encapsulation.CIPIdentityItem Identity { get; set; }

        private CancellationTokenSource? TcpListenerCancellation;
        private CancellationTokenSource? UdpListenerCancellation;

        public EeipDeviceMock(Uri uri, int eeipPort = 44818)
        {
            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException("EeipDeviceMock: Address must be absolute", nameof(uri));
            }
            ushort port;
            if (!uri.IsDefaultPort)
            {
                port = (ushort)uri.Port;
            }
            else
            {
                if (uri.Scheme.Equals("ethernet-ip-1", StringComparison.OrdinalIgnoreCase))
                {
                    port = 0x08AE;
                }
                else
                {
                    // ethernet-ip-2 or default
                    port = 0xAF12;
                }
            }

            DeviceIp = Dns.GetHostAddresses(uri.Host)
                .First(x => x.AddressFamily == AddressFamily.InterNetwork);

            EeipPort = port;
            StartEeipUdpListener();
            StartTcpListener();
        }

        private void StartEeipUdpListener()
        {
            UdpListenerCancellation = new CancellationTokenSource();
            Task.Run(() =>
            {
                Debug.WriteLine($"EeipDeviceMock: Start listening on UDP port 0x{EeipPort.ToString("X")}");
                IPEndPoint udpEndPoint = new IPEndPoint(IPAddress.Any, EeipPort);
                using UdpClient udpClient = new UdpClient(udpEndPoint);
                Connected = true;
                try
                {
                    while (!UdpListenerCancellation.IsCancellationRequested)
                    {
                        byte[] receivedBytes;
                        IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        try
                        {
                            receivedBytes = udpClient.Receive(ref clientEndPoint);
                            Debug.WriteLine($"EeipDeviceMock: Received a UDP package with {receivedBytes.Length} bytes from {clientEndPoint.Address}:0x{clientEndPoint.Port.ToString("X")}");

                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"EeipDeviceMock: Failed to listen for broadcasts on UDP port 0x{EeipPort.ToString("X")}: " + ex.Message);
                            throw;
                        }

                        try
                        {
                            Encapsulation? reply = HandleReceivedEeipRequest(receivedBytes, udpClient.Client.ProtocolType);
                            if (reply != null)
                            {
                                byte[] serializedReply = reply.SerializeToBytes();
                                udpClient.Send(serializedReply, serializedReply.Length, clientEndPoint);
                            }
                        }
                        catch (Exception sendException)
                        {
                            Debug.WriteLine($"EeipDeviceMock: Failed to send reply on UDP port 0x{EeipPort.ToString("X")}: " + sendException.Message);
                            throw;
                        }
                    }
                }
                finally
                {
                    udpClient.Close();
                }
            }, this.UdpListenerCancellation.Token);
        }

        private void StopUdpListener()
        {
            UdpListenerCancellation?.Cancel();
        }

        private void StartTcpListener()
        {
            TcpListenerCancellation = new CancellationTokenSource();

            Task.Run(() =>
            {
                IPEndPoint endPoint = new IPEndPoint(DeviceIp, EeipPort);

                TcpListener? tcpListener = null;
                TcpClient? tcpClient = null;
                try
                {
                    tcpListener = new TcpListener(endPoint);
                    tcpListener.Start();
                    Debug.WriteLine($"EeipDeviceMock: Start listening on TCP port 0x{EeipPort.ToString("X")}");
                    while (!TcpListenerCancellation.IsCancellationRequested)
                    {
                        tcpClient = tcpListener.AcceptTcpClient();
                        Connected = true;
                        Debug.WriteLine($"EeipDeviceMock: Accepted TCP connection from {endPoint.Address} on port 0x{endPoint.Port.ToString("X")}");

                        var tcpStream = tcpClient.GetStream();
                        var receiveBuffer = new byte[1024];
                        var receiveBufferSpan = new Span<byte>(receiveBuffer);
                        while (!TcpListenerCancellation.IsCancellationRequested && Connected)
                        {
                            int bytesReceived = tcpStream.Read(receiveBufferSpan);
                            Encapsulation? reply = HandleReceivedEeipRequest(receiveBufferSpan, tcpClient.Client.ProtocolType);
                            if (reply != null)
                            {
                                tcpStream.Write(reply.SerializeToBytes());
                            }
                        }
                        Debug.WriteLine($"EeipDeviceMock: Disconnect TCP from {endPoint.Address} on port 0x{endPoint.Port.ToString("X")}");
                        Connected = false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"EeipDeviceMock: Failed to listen on TCP port 0x{EeipPort.ToString("X")}: " + ex.Message);
                }
                finally
                {
                    SessionRegistered = false;
                    Connected = false;
                    tcpClient?.Close();
                    Debug.WriteLine($"EeipDeviceMock: Closing TCP listener.");
                    tcpClient?.Dispose();
                }
            }, this.TcpListenerCancellation.Token);
        }

        private void StopTcpListener(Socket? listener)
        {
            TcpListenerCancellation?.Cancel();
        }

        private Encapsulation? HandleReceivedEeipRequest(Span<byte> receiveBuffer, ProtocolType protocolType)
        {
            var scannerRequest = EEIPTestExtensions.ExpandEncapsulation(receiveBuffer);
            Encapsulation? reply = null;
            switch (scannerRequest.Command)
            {
                case Encapsulation.CommandsEnum.NOP:
                    break;
                case Encapsulation.CommandsEnum.RegisterSession:
                    Debug.WriteLine("EeipDeviceMock: Received Register Session request.");
                    reply = HandleRegisterSessionRequest(scannerRequest);
                    break;
                case Encapsulation.CommandsEnum.UnRegisterSession:
                    Debug.WriteLine("EeipDeviceMock: Received Unregister Session request.");
                    reply = HandleUnRegisterSessionRequest(scannerRequest);
                    break;
                case Encapsulation.CommandsEnum.ListServices:
                    Debug.WriteLine("EeipDeviceMock: Received unimplemented List Services request.");
                    break;
                case Encapsulation.CommandsEnum.ListIdentity:
                    Debug.WriteLine("EeipDeviceMock: Received List Identity request.");
                    reply = HandleListIdentityRequest(scannerRequest, protocolType);
                    break;
                case Encapsulation.CommandsEnum.ListInterfaces:
                    Debug.WriteLine("EeipDeviceMock: Received unimplemented List Interfaces request.");
                    break;
                case Encapsulation.CommandsEnum.SendRRData:
                    Debug.WriteLine("EeipDeviceMock: Received unimplemented Send RR Data request.");
                    break;
                case Encapsulation.CommandsEnum.SendUnitData:
                    Debug.WriteLine("EeipDeviceMock: Received unimplemented Send Unit Data request.");
                    break;
                case Encapsulation.CommandsEnum.IndicateStatus:
                    Debug.WriteLine("EeipDeviceMock: Received unimplemented Indicate Status request.");
                    break;
                case Encapsulation.CommandsEnum.Cancel:
                    Debug.WriteLine("EeipDeviceMock: Received unimplemented Cancel request.");
                    break;
                default:
                    Debug.WriteLine("EeipDeviceMock: Received undefined request.");
                    break;
            }
            return reply;
        }

        private Encapsulation HandleRegisterSessionRequest(Encapsulation scannerRequest)
        {
            EeipRegisterSessionCommandData commandData = EeipRegisterSessionCommandData.Expand(new Span<byte>(scannerRequest.CommandSpecificData.ToArray()));

            if (SessionRegistered)
            {
                EncapsRegisterSessionReply alreadyRegisterdErrorReply = new EncapsRegisterSessionReply(Encapsulation.StatusEnum.InvalidCommand, 0, scannerRequest.SenderContext);
                //SessionSocket?.Send(alreadyRegisterdErrorReply.SerializeToBytes());
                return alreadyRegisterdErrorReply;
            }
            if ((commandData.EncapsulationProtocolVersion != EncapsulationProtocolVersion) || (commandData.Options != 0))
            {
                EncapsRegisterSessionReply unsupportedProtocolReply = new EncapsRegisterSessionReply(Encapsulation.StatusEnum.UnsupportedEncapsulationProtocol, 0, scannerRequest.SenderContext);
                //SessionSocket?.Send(unsupportedProtocolReply.SerializeToBytes());
                return unsupportedProtocolReply;
            }
            Random random = new Random();
            SessionHandle = (uint)random.Next();
            EncapsRegisterSessionReply registerReply = new EncapsRegisterSessionReply(sessionHandlle: SessionHandle, senderContext: scannerRequest.SenderContext);
            //SessionSocket?.Send(registerReply.SerializeToBytes());
            SessionRegistered = true;
            Debug.WriteLine($"EeipDeviceMock: Registered session with handle {SessionHandle}.");
            return registerReply;

        }
        private Encapsulation? HandleUnRegisterSessionRequest(Encapsulation scannerRequest)
        {
            SessionRegistered = false;
            Connected = false;
            return null;
        }


        private Encapsulation? HandleListIdentityRequest(Encapsulation scannerRequest, ProtocolType protocolType)
        {
            if (protocolType == ProtocolType.Udp)
            {
                ushort maxDelay = BinaryPrimitives.ReadUInt16LittleEndian(scannerRequest.SenderContext);
                if (maxDelay < 500)
                {
                    maxDelay = 500;
                }
                var delay = new Random().Next(maxDelay);
                Thread.Sleep(delay);
            }
            Encapsulation listIdentityReply = new EncapsListIdentityReply(Identity);

            return listIdentityReply;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    TcpListenerCancellation?.Cancel();
                    UdpListenerCancellation?.Cancel();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
