using FluentAssertions;
using NUnit.Framework;
using Sres.Net.EEIP;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace Sres.Net.EEIP.Tests
{
    [TestFixture]
    public class RegisterSessionTests
    {

        [Test]
        public async Task RegisterSessionShallSucceed()
        {
            // Arrange
            var port = TestHelper.GetAvailablePort();
            using var eeipDevice = new EeipDeviceMock(port);
            using EEIPClient eeipClient = new Sres.Net.EEIP.EEIPClient();
            Uri deviceUri = new UriBuilder("tcp", "localhost", port).Uri;

            // Act
            var sessionHandle = await eeipClient.RegisterSessionAsync(deviceUri);
        }

        [Test]
        public async Task RegisterSessionTimeOut()
        {
            // Arrange
            var port = TestHelper.GetAvailablePort();
            using var eeipDevice = new EeipDeviceMock(port);
            using EEIPClient eeipClient = new Sres.Net.EEIP.EEIPClient();
            Uri deviceUri = new UriBuilder("tcp", "localhost", port).Uri;

            // Act
            var sessionHandle = await eeipClient.RegisterSessionAsync(deviceUri);

        }
    }
}
