using EEIP.Tests;
using NUnit.Framework;
using Sres.Net.EEIP;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sres.Net.EEIP.Tests
{
    [TestFixture()]
    public class CommonPacketFormatTests
    {
        [Test()]
        public void SerializeToBytesTest()
        {

            // Arrange
            var seed = TestHelper.CreateRandomSeed();
            var random = new Random(seed);

            Span<byte> commonPacketFormatSpan = new Span<byte>();

            throw new NotImplementedException();
        }
    }
}
