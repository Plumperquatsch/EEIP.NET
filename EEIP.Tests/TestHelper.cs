using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace Sres.Net.EEIP.Tests
{
    public class TestHelper
    {
        private static object testHelperLock = new object();

        public static int CreateRandomSeed()
        {
            int seed = new Random().Next();
            Console.WriteLine($"Using random seed {seed}");
            return seed;
        }

        /// <summary>
        /// Find a free port on local host.
        /// </summary>
        /// <param name="startingPort">Min port number to start looking for a free port</param>
        /// <returns>Free port on local host</returns>
        /// <remarks>Source: https://gist.github.com/jrusbatch/4211535 </remarks>
        public static int GetAvailablePort(int startingPort = 30000)
        {
            lock (testHelperLock)
            {
                var properties = IPGlobalProperties.GetIPGlobalProperties();

                //getting active connections
                var tcpConnectionPorts = properties.GetActiveTcpConnections()
                                    .Where(n => n.LocalEndPoint.Port >= startingPort)
                                    .Select(n => n.LocalEndPoint.Port);

                //getting active TCP listeners - WCF service listening in TCP
                var tcpListenerPorts = properties.GetActiveTcpListeners()
                                    .Where(n => n.Port >= startingPort)
                                    .Select(n => n.Port);

                //getting active UDP listeners
                var udpListenerPorts = properties.GetActiveUdpListeners()
                                    .Where(n => n.Port >= startingPort)
                                    .Select(n => n.Port);

                var port = Enumerable.Range(startingPort, ushort.MaxValue)
                    .Where(i => !tcpConnectionPorts.Contains(i))
                    .Where(i => !tcpListenerPorts.Contains(i))
                    .Where(i => !udpListenerPorts.Contains(i))
                    .FirstOrDefault();

                return port; 
            }
        }
    }

}
