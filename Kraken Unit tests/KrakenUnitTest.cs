using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Kraken_Unit_tests
{
    [TestClass]
    public class KrakenUnitTest
    {
        [TestMethod]
        public void CompressImage()
        {
            // Arrange
            var imageFile = Environment.CurrentDirectory + "\\Kraken.png";
            // Act
            var result = Kraken.Kraken.Compress(imageFile, true);
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.success);
        }
    }
}
