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
        public bool Connected { get; private set; }
        public bool SessionRegistered { get; private set; }
        public uint SessionHandle { get; private set; }

        public int EeipPort { get; private set; }
        private Task EeipListener { get; set; }

        private CancellationTokenSource ListenerCancellation { get; set; }
        public Socket SessionSocket { get; private set; }


        public EeipDeviceMock(int eeipPort = 44818)
        {
            EeipPort = eeipPort;
            StartListener();
        }

        private void StartListener()
        {
            ListenerCancellation = new CancellationTokenSource();
            EeipListener = Task.Run(() =>
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, EeipPort);

                while (!ListenerCancellation.IsCancellationRequested)
                {
                    try
                    {
                        //Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Unspecified);
                        listener.Bind(localEndPoint);
                        listener.Listen(10);
                        Debug.WriteLine("EeipDeviceMock starts listening on TCP");

                        SessionSocket = listener.Accept();
                        Connected = true;

                        while (!ListenerCancellation.IsCancellationRequested)
                        {
                            byte[] receiveBuffer = new byte[1024];
                            int bytesReceived = SessionSocket.Receive(receiveBuffer);
                            HandleReceivedEeipRequest(receiveBuffer); 
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Failed to listen on TCP port: " + ex.Message);
                        Connected = false;
                    }
                }
            }, ListenerCancellation.Token);
        }

        private void HandleReceivedEeipRequest(byte[] receiveBuffer)
        {
            Debug.WriteLine($"EeipDeviceMock received {receiveBuffer.Count()} bytes data from socket.");
            if (!SessionRegistered)
            {
                Encapsulation scannerRequest = EEIPTestExtensions.ExpandEncapsulation(receiveBuffer);
                if(scannerRequest.Command == Encapsulation.CommandsEnum.RegisterSession)
                {
                    Debug.WriteLine("Session registered.");
                    ConfirmSessionRegistration(scannerRequest.SenderContext);
                    SessionRegistered = true;
                }
            }
        }

        private void ConfirmSessionRegistration(byte[] senderContext)
        {
            Random random = new Random();
            SessionHandle = (uint)random.Next();
            EncapsRegisterSessionReply registerReply = new EncapsRegisterSessionReply(sessionHandlle: SessionHandle, senderContext:senderContext);
            SessionSocket.Send(registerReply.SerializeToBytes());
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ListenerCancellation.Cancel();
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
