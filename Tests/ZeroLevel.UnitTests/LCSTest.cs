using Xunit;
using ZeroLevel.Semantic;

namespace ZeroLevel.UnitTests
{
    public class LCSTest
    {
        [Fact]
        public void LCSTest1()
        {
            // Arrange
            var s_base = "abcdefghijklmnopqrstuvwxyz";
            var s1 = "klmnO";

            var s2 = "";
            string s3 = null;

            // Act
            var st11 = LongestCommonSubstring.LCS(s_base, s1);
            var st12 = LongestCommonSubstring.LCS(s1, s_base);

            var st13 = LongestCommonSubstring.LCSIgnoreCase(s_base, s1);
            var st14 = LongestCommonSubstring.LCSIgnoreCase(s1, s_base);

            var st21 = LongestCommonSubstring.LCS(s_base, s2);
            var st22 = LongestCommonSubstring.LCS(s2, s_base);

            var st31 = LongestCommonSubstring.LCS(s_base, s3);
            var st32 = LongestCommonSubstring.LCS(s3, s_base);

            //Assert
            Assert.Equal("klmn", st11);
            Assert.Equal("klmn", st12);

            Assert.Equal(s1, st13, ignoreCase: true);
            Assert.Equal(s1, st14, ignoreCase: true);


            Assert.Equal(string.Empty, st21);
            Assert.Equal(string.Empty, st22);
                         
            Assert.Equal(string.Empty, st31);
            Assert.Equal(string.Empty, st32);

        }
    }
}
