using FluentAssertions;
using NUnit.Framework;
using System;
using System.Text;
using static Sres.Net.EEIP.Encapsulation;

namespace EEIP.Tests
{
    public class CIPIdentityItemTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CIPIdentityItemTestTwoItemsShallBeEqual()
        {
            // Arrange
            var seed = TestHelper.CreateRandomSeed();
            var random = new Random(seed);
            int startingByte = 7;
            int itemCountOffset = 2;
            int idendityDataLength = 37;
            string productTestName = "test product";
            var productTestNameData = Encoding.ASCII.GetBytes(productTestName);
            var identityData = new byte[startingByte + itemCountOffset + idendityDataLength + productTestNameData.Length + 2];
            random.NextBytes(identityData);
            Array.Copy(productTestNameData, 0, identityData, 37 + startingByte + itemCountOffset, productTestNameData.Length);
            identityData[36 + startingByte + itemCountOffset] = (byte)productTestNameData.Length;

            var identityItem1 = CIPIdentityItem.Deserialize(startingByte, identityData);
            var identityItem2 = CIPIdentityItem.Deserialize(startingByte, identityData);

            // Act
            identityItem1.Should().Be(identityItem2);
        }
    }
}
