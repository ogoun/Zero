using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ZeroLevel.Services.Semantic;

namespace ZeroLevel.UnitTests
{
    public class SemanticTests
    {
        [Fact]
        public void WordTokenizerTest()
        {
            // Arrange            
            var line = "Хорошее понимание проекций, отражений и векторных операций (как в истинном значении скалярного (dot) и векторного (cross) произведений векторов) обычно приходит с растущим чувством беспокойства при использованием тригонометрии. ";
            var test = new string[] {
                "хорошее", "понимание", "проекций", "отражений", "и"
                , "векторных", "операций", "как", "в", "истинном"
                , "значении", "скалярного","dot","и","векторного","cross","произведений"
                ,"векторов","обычно","приходит","с","растущим","чувством","беспокойства"
                ,"при","использованием", "тригонометрии"};
            // Act
            var terms = WordTokenizer.Tokenize(line).ToArray();
            // Assert

            Assert.True(test.Length == terms.Length);
            for (int i = 0; i < terms.Length; i++)
            {
                Assert.True(string.CompareOrdinal(test[i], terms[i]) == 0);
            }
        }
    }
}
