using ZeroLevel.Services.Semantic;
using ZeroLevel.Services.Serialization;
using ZeroLevel.Sleopok.Engine;
using ZeroLevel.Sleopok.Engine.Models;
using ZeroLevel.Sleopok.Engine.Services;
using ZeroLevel.Sleopok.Engine.Services.Storage;

namespace Sleopok.Tests
{
    internal class Program
    {
        public sealed class BookDocument
        {
            public string Id { get; set; }

            [SleoIndex("title", 200.0f)]
            public string Title { get; set; }

            [SleoIndex("titlelm", 100.0f)]
            public string TitleLemmas { get; set; }

            [SleoIndex("author", 10.0f)]
            public string Author { get; set; }

            [SleoIndex("genre", 1.0f)]
            public string Genre { get; set; }
        }

        private static Dictionary<string, string> _titles = new Dictionary<string, string>
        {
            { "66056bc0481e83af64c55022", "Документ без названия" },
            { "6605698d481e83af64c45ad7", "На развилке дорог. Часть 2"},
            { "660581bc481e83af64cb8b4d", "Паниклав"},
            { "66057aa2481e83af64c9bb11", "Князь. Война магов (сборник)"},
            { "66057f75481e83af64cb04f7", "Антология севетского детектива-8. Компиляция. Книги 1-17"},
            { "66057bd4481e83af64ca0779", "Вор черной масти"},
            { "66057247481e83af64c76860", "Выбор"},
            { "66056807481e83af64c3a64f", "Последняя лекция"},
            { "66057f13481e83af64caed5d", "Оружие Круппа. История династии пушечных королей"},
            { "66057a37481e83af64c9a14b", "Месть Черного Дракона"},
            { "660588e8481e83af64cd2d3e", "Мгла над старыми могилами"},
            { "66056e88481e83af64c64e81", "Кровь и железо"},
            { "66057a8e481e83af64c9b673", "Маленькая страна"},
            { "6605687d481e83af64c3e360", "Санкт-Петербург – история в преданиях и легендах"},
            { "66057987481e83af64c9770c", "Контракт на рабство"},
            { "66059052481e83af64cf5e31", "Агент космического сыска"},
            { "660580f9481e83af64cb61c9", "Две жизни Алессы Коэн"},
            { "66056807481e84af64c3a64f", "Последняя история"},
            { "66057f13481e85af64caed5d", "История Китая"},
            { "66057a37481e86af64c9a14b", "Время Черного Дракона"},
            { "660588e8481e87af64cd2d3e", "Страна которой нет"},
        };

        static async Task Main(string[] args)
        {
            // TestCompression();
            // await FillOneFieldIndex();
            // await TestSearch();
            await TestEngine();
        }

        static async Task TestEngine()
        {
            var engine = new SleoEngine<BookDocument>(@"H:\Test", b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                builder.Write(new[]
                {
                    new BookDocument{ Id = "01", Title = "Страж птица",  },
                    new BookDocument{ Id = "02" },
                    new BookDocument{ Id = "03" },
                    new BookDocument{ Id = "04" },
                });
            }
        }

        static void TestCompression()
        {
            var strings = new string[]
            {
                string.Empty,
                "doc1",
                "doc2",
                "",
                " ",
                "\r\n",
                "last",
                "doc3",
                "doc4",
                "doc5",
                "doc6",
                "doc7",
                "doc8",
                "doc9",
                "doc10",
            };

            var clearbytes = MessageSerializer.SerializeCompatible(strings).Length;
            var compressed = Compressor.Compress(strings);
            Console.WriteLine($"{compressed.Length} / {clearbytes} bytes");

            var decomressed = Compressor.DecompressToDocuments(compressed);
            int index = 0;
            foreach (var s in decomressed)
            {
                if (!(string.IsNullOrEmpty(s) && string.IsNullOrEmpty(strings[index])) && 0 != string.CompareOrdinal(strings[index], s))
                {
                    Console.WriteLine($"Got {s}. Expected {strings[index]}");
                }
                index++;
            }
        }

        static async Task FillOneFieldIndex()
        {
            var store = new DataStorage(@"H:\TEST");
            using (var writer = store.GetWriter("title"))
            {
                foreach (var kv in _titles)
                {
                    var tokens = WordTokenizer.Tokenize(kv.Value);
                    foreach (var t in tokens)
                    {
                        await writer.Write(t, kv.Key);
                    }
                }
                await writer.Complete();
            }
        }

        static async Task TestSearch()
        {
            var store = new DataStorage(@"H:\TEST");
            var docs = await store.GetDocuments("title", new string[] { "кровь", "страна", "железо", "история", "оружие" }, 1.0f, false);
            foreach (var kv in docs.OrderByDescending(kv => kv.Value))
            {
                Console.WriteLine($"[{kv.Key}: {kv.Value}] {_titles[kv.Key]}");
            }
        }
    }
}
