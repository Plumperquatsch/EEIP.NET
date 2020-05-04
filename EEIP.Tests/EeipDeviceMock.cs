using System;
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

        private CancellationTokenSource? ListenerCancellation;
        private Socket? SessionSocket;
        private Task? EeipListener;

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
            StartEeipListener();
        }

        private void StartEeipUdpListener()
        {
            UdpListenerCancellation = new CancellationTokenSource();
            this.BroadcastEeipListener = Task.Run(() =>
            {
                Debug.WriteLine($"EeipDeviceMock: Start listening for broadcasts on UDP port 0x{EeipPort.ToString("X")}");
                IPEndPoint udpEndPoint = new IPEndPoint(IPAddress.Any, EeipPort);
                using UdpClient udpListener = new UdpClient(udpEndPoint);
                Connected = true;
                try
                {
                    while (!UdpListenerCancellation.IsCancellationRequested)
                    {
                        IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, EeipPort);
                        byte[] receivedBytes = udpListener.Receive(ref clientEndPoint);
                        Debug.WriteLine($"EeipDeviceMock: Received a UDP package with {receivedBytes.Length} bytes from {clientEndPoint.Address}");
                        HandleReceivedEeipRequest(receivedBytes);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"EeipDeviceMock: Failed to listen for broadcasts on UDP port 0x{EeipPort.ToString("X")}: " + ex.Message);
                }
                finally
                {
                    udpListener.Close();
                }
            }, this.UdpListenerCancellation.Token);
        }

        private void StartEeipListener()
        {
            ListenerCancellation = new CancellationTokenSource();

            this.EeipListener = Task.Run(() =>
            {
                //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                //IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(DeviceIp, EeipPort);

                Socket? listener = null;
                try
                {
                    while (!ListenerCancellation.IsCancellationRequested)
                    {
                        listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Unspecified);
                        listener.Bind(localEndPoint);
                        listener.Listen(1);
                        Debug.WriteLine($"EeipDeviceMock: Start listening on TCP port 0x{EeipPort.ToString("X")}");

                        SessionSocket = listener.Accept();
                        if (SessionSocket != null)
                        {
                            Connected = true;

                            while (!ListenerCancellation.IsCancellationRequested && SessionSocket.Connected)
                            {
                                byte[] receiveBuffer = new byte[1024];
                                int bytesReceived = SessionSocket.Receive(receiveBuffer);
                                HandleReceivedEeipRequest(receiveBuffer);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"EeipDeviceMock: Failed to listen on TCP port 0x{EeipPort.ToString("X")}: " + ex.Message);
                }
                finally
                {
                    listener = StopListener(listener);
                }
            }, this.ListenerCancellation.Token);
        }

        private Socket? StopListener(Socket? listener)
        {
            SessionRegistered = false;
            Connected = false;
            listener?.Dispose();
            SessionSocket?.Dispose();
            listener = null;
            SessionSocket = null;
            return listener;
        }

        private void HandleReceivedEeipRequest(byte[] receiveBuffer)
        {
            Debug.WriteLine($"EeipDeviceMock: Received {receiveBuffer.Count()} bytes data from socket.");
            var scannerRequest = EEIPTestExtensions.ExpandEncapsulation(receiveBuffer);
            switch (scannerRequest.Command)
            {
                case Encapsulation.CommandsEnum.NOP:
                    break;
                case Encapsulation.CommandsEnum.RegisterSession:
                    Debug.WriteLine("EeipDeviceMock: Received Register Session request.");
                    HandleRegisterSessionRequest(scannerRequest);
                    break;
                case Encapsulation.CommandsEnum.UnRegisterSession:
                    Debug.WriteLine("EeipDeviceMock: Received Unregister Session request.");
                    HandleUnRegisterSessionRequest(scannerRequest);
                    break;
                case Encapsulation.CommandsEnum.ListServices:
                    Debug.WriteLine("EeipDeviceMock: Received unimplemented List Services request.");
                    break;
                case Encapsulation.CommandsEnum.ListIdentity:
                    Debug.WriteLine("EeipDeviceMock: Received List Identity request.");
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
                    Debug.WriteLine("EeipDeviceMock: Received unimplementd Cancel request.");
                    break;
                default:
                    Debug.WriteLine("EeipDeviceMock: Received undefined request.");
                    break;
            }
        }

        private void HandleRegisterSessionRequest(Encapsulation scannerRequest)
        {
            EeipRegisterSessionCommandData commandData = EeipRegisterSessionCommandData.Expand(new Span<byte>(scannerRequest.CommandSpecificData.ToArray()));

            if (SessionRegistered)
            {
                EncapsRegisterSessionReply alreadyRegisterdErrorReply = new EncapsRegisterSessionReply(Encapsulation.StatusEnum.InvalidCommand, 0, scannerRequest.SenderContext);
                SessionSocket?.Send(alreadyRegisterdErrorReply.SerializeToBytes());
            }
            if ((commandData.EncapsulationProtocolVersion != EncapsulationProtocolVersion) || (commandData.Options != 0))
            {
                EncapsRegisterSessionReply unsupportedProtocolReply = new EncapsRegisterSessionReply(Encapsulation.StatusEnum.UnsupportedEncapsulationProtocol, 0, scannerRequest.SenderContext);
                SessionSocket?.Send(unsupportedProtocolReply.SerializeToBytes());
            }
            Random random = new Random();
            SessionHandle = (uint)random.Next();
            EncapsRegisterSessionReply registerReply = new EncapsRegisterSessionReply(sessionHandlle: SessionHandle, senderContext: scannerRequest.SenderContext);
            SessionSocket?.Send(registerReply.SerializeToBytes());
            SessionRegistered = true;
            Debug.WriteLine($"EeipDeviceMock: Registered session with handle {SessionHandle}.");

        }
        private void HandleUnRegisterSessionRequest(Encapsulation scannerRequest)
        {
            ListenerCancellation?.Cancel();
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ListenerCancellation?.Cancel();
                    SessionSocket?.Dispose();
                    SessionSocket = null;
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
