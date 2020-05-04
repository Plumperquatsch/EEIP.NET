using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sres.Net.EEIP.Tests
{
    [TestFixture]
    public class ListIdentityTests
    {
        [Test]
        public async Task ListIdentityShallFindDeviceMock()
        {
            // Arrange
            var port = TestHelper.GetAvailablePort();
            Uri deviceUri = new UriBuilder("tcp", "localhost", port).Uri;
            using var eeipDevice = new EeipDeviceMock(deviceUri);
            using EEIPClient eeipClient = new Sres.Net.EEIP.EEIPClient();

            // Act
            var identity = await eeipClient.ListIdentityAsync((ushort)port);
            identity.Should().NotBeNull();
            identity.Should().NotBeEmpty();
        }
    }
}
