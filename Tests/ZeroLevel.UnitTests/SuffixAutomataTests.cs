using Xunit;
using ZeroLevel.Semantic;
using ZeroLevel.Services.Text;

namespace ZeroLevel.UnitTests
{
    public class SuffixAutomataTests
    {
        [Fact]
        public void IsSubstringTest()
        {
            var text = @"=Однако эту оценку легко показать и без знания алгоритма. Вспомним о том, что число состояний равно количеству различных значений множеств endpos.#";
            var automata = new SuffixAutomata();
            automata.Init();
            foreach (var ch in text)
            {
                automata.Extend(ch);
            }
            Assert.True(automata.IsSubstring("Вспомним"));
            Assert.True(automata.IsSubstring("")); // empty line
            Assert.True(automata.IsSubstring("#")); // end line
            Assert.True(automata.IsSubstring("=")); // start line
            Assert.True(automata.IsSubstring(null)); // null
            Assert.False(automata.IsSubstring("равноценно"));
            Assert.False(automata.IsSubstring("нетслова"));
        }

        [Fact]
        public void IntersectionTest()
        {
            var text = @"=Однако эту оценку легко показать и без знания алгоритма. Вспомним о том, что число состояний равно количеству различных значений множеств endpos.#";
            var i = LongestCommonSubstring.LCS(text, "енк");
            Assert.Equal("енк", i);

            i = LongestCommonSubstring.LCS(text, "стоя");
            Assert.Equal("стоя", i);

            i = LongestCommonSubstring.LCS(text, "горизонт");
            Assert.Equal("гори", i);
        }
    }
}
