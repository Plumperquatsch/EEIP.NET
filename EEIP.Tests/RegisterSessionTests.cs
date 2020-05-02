using FluentAssertions;
using NUnit.Framework;
using Sres.Net.EEIP;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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
            
            // Assert
            sessionHandle.Should().Be(eeipDevice.SessionHandle);
            eeipDevice.Connected.Should().BeTrue();
            eeipDevice.SessionRegistered.Should().BeTrue();
        }

        [Test]
        public async Task RetryRegisterSessionAfterTimeOutShallSucceed()
        {
            // Arrange
            var port = TestHelper.GetAvailablePort();
            using EEIPClient eeipClient = new Sres.Net.EEIP.EEIPClient();
            Uri deviceUri = new UriBuilder("tcp", "localhost", port).Uri;

            uint sessionHandle;
            try
            {
                sessionHandle = await eeipClient.RegisterSessionAsync(deviceUri);
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"First connection failed as intended: {ex.Message}");
            }

            // Act
            using var eeipDevice = new EeipDeviceMock(port);
            sessionHandle = await eeipClient.RegisterSessionAsync(deviceUri);
            
            // Assert
            sessionHandle.Should().Be(eeipDevice.SessionHandle);
            eeipDevice.Connected.Should().BeTrue();
            eeipDevice.SessionRegistered.Should().BeTrue();
        }
    }
}
