using FluentAssertions;
using NUnit.Framework;
using Sres.Net.EEIP;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;
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
            Uri deviceUri = new UriBuilder("tcp", "localhost", port).Uri;
            using var eeipDevice = new EeipDeviceMock(deviceUri);
            using EEIPClient eeipClient = new Sres.Net.EEIP.EEIPClient();

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
            using var eeipDevice = new EeipDeviceMock(deviceUri);
            sessionHandle = await eeipClient.RegisterSessionAsync(deviceUri);
            
            // Assert
            sessionHandle.Should().Be(eeipDevice.SessionHandle);
            eeipDevice.Connected.Should().BeTrue();
            eeipDevice.SessionRegistered.Should().BeTrue();
        }

        [Test]
        public async Task UnRegisterSessionShallSucceed()
        {
            // Arrange
            var port = TestHelper.GetAvailablePort();
            Uri deviceUri = new UriBuilder("tcp", "localhost", port).Uri;
            using var eeipDevice = new EeipDeviceMock(deviceUri);
            using EEIPClient eeipClient = new Sres.Net.EEIP.EEIPClient();

            var sessionHandle = await eeipClient.RegisterSessionAsync(deviceUri);
            sessionHandle.Should().Be(eeipDevice.SessionHandle);
            eeipDevice.Connected.Should().BeTrue();
            eeipDevice.SessionRegistered.Should().BeTrue();

            // Act
            await eeipClient.UnRegisterSessionAsync();
            
            // Assert
            Thread.Sleep(100);
            eeipDevice.Connected.Should().BeFalse();
            eeipDevice.SessionRegistered.Should().BeFalse();
        }

    }
}
