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

        public int EeipPort { get; private set; }

        private CancellationTokenSource? ListenerCancellation;
        private Socket? SessionSocket;
        private Task? EeipListener;

        public EeipDeviceMock(int eeipPort = 44818)
        {
            EeipPort = eeipPort;
            StartListener();
        }

        private void StartListener()
        {
            ListenerCancellation = new CancellationTokenSource();
            this.EeipListener = Task.Run(() =>
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, EeipPort);

                while (!ListenerCancellation.IsCancellationRequested)
                {
                    Socket? listener = null;
                    try
                    {
                        listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Unspecified);
                        listener.Bind(localEndPoint);
                        listener.Listen(10);
                        Debug.WriteLine("EeipDeviceMock starts listening on TCP");

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
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Failed to listen on TCP port: " + ex.Message);
                    }
                    finally
                    {
                        listener = StopListener(listener);
                    }
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
            Debug.WriteLine($"EeipDeviceMock received {receiveBuffer.Count()} bytes data from socket.");
            var scannerRequest = EEIPTestExtensions.ExpandEncapsulation(receiveBuffer);
            switch (scannerRequest.Command)
            {
                case Encapsulation.CommandsEnum.NOP:
                    break;
                case Encapsulation.CommandsEnum.RegisterSession:
                    Debug.WriteLine("Register Session.");
                    HandleRegisterSessionRequest(scannerRequest);
                    break;
                case Encapsulation.CommandsEnum.UnRegisterSession:
                    Debug.WriteLine("Unregister Session.");
                    HandleUnRegisterSessionRequest(scannerRequest);
                    break;
                case Encapsulation.CommandsEnum.ListServices:
                    break;
                case Encapsulation.CommandsEnum.ListIdentity:
                    break;
                case Encapsulation.CommandsEnum.ListInterfaces:
                    break;
                case Encapsulation.CommandsEnum.SendRRData:
                    break;
                case Encapsulation.CommandsEnum.SendUnitData:
                    break;
                case Encapsulation.CommandsEnum.IndicateStatus:
                    break;
                case Encapsulation.CommandsEnum.Cancel:
                    break;
                default:
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
