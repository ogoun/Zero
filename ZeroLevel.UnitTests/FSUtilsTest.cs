using System.Collections.Generic;
using Xunit;
using ZeroLevel.Services.FileSystem;

namespace ZeroLevel.UnitTests
{
    public class FSUtilsTest
    {
        [Fact]
        public void FileNameCorrectionTest()
        {
            // Arrange            
            var validNames = new List<string> { "CON1", "a.cOn.a", "$PRN", "LPT10", "lpt10.txt", "COM11.ee" };
            var invalidNames = new List<string> { "CON", "cOn", "PRN", "LPT4", "LPT4.", "LPT4.txt", "COM1.ee" };
            var invalidRootNames = new List<string> { "$mft", "$mftmirr", "$logfile", "$volume", "$attrdef", "$bitmap", "$boot", "$badclus", "$secure", "$upcase", "$extend", "$quota", "$objid", "$reparse" };
            // Act

            // Assert
            foreach (var validName in validNames)
            {
                Assert.Equal(validName, FSUtils.FileNameCorrection(validName));
            }
            foreach (var invalidName in invalidNames)
            {
                Assert.NotEqual(invalidName, FSUtils.FileNameCorrection(invalidName));
            }
            foreach (var invalidName in invalidRootNames)
            {
                Assert.NotEqual(invalidName, FSUtils.FileNameCorrection(invalidName, true));
            }
        }
    }
}
