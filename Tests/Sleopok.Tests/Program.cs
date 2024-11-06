using Iveonik.Stemmers;
using ZeroLevel;
using ZeroLevel.Services.FileSystem;
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
        public sealed class BookDocumentSimple
        {
            public string Id { get; set; }

            [SleoIndex("title", 10.0f, avaliableForExactMatch: true)]
            public string Title { get; set; }

            [SleoIndex("author", 10.0f, avaliableForExactMatch: true)]
            public string Author { get; set; }
        }

        public sealed class BookDocument
        {
            public string Id { get; set; }

            [SleoIndex("title", 10.0f, avaliableForExactMatch: true)]
            public string Title { get; set; }

            [SleoIndex("stemms", 2.0f)]
            public string Stemms { get; set; }

            [SleoIndex("author", 10.0f, avaliableForExactMatch: true)]
            public string Author { get; set; }
        }

        static async Task Main(string[] args)
        {
            //TestCompression();
            // await TestSearch();
            // await TestEngine();
            await TestEngineReadWrite();
        }

        static async Task TestEngineReadWrite()
        {
            ILexProvider lexProvider = new LexProvider(new RussianStemmer());
            var tempFolder = Path.Combine(Configuration.BaseDirectory, "SleoTestStorage");
            FSUtils.CleanAndTestFolder(tempFolder);
            var lex = new Func<string, string>(s => string.Join(" ", lexProvider.ExtractUniqueLexTokens(s).Select(s => s.Token)));
            var engine = new SleoEngine<BookDocumentSimple>(tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[]
                {
                    //new BookDocument { Id = "01", Title = "Юность Пушкина", Author = "Егорова Елена", Stemms = lex("Юность Пушкина") },
                    new BookDocumentSimple { Id = "01", Title = "Стихи Не Для Дам", Author = "Пушкин Александр Сергеевич" },
                    new BookDocumentSimple { Id = "02", Title = "Светлинен стих", Author = "Азимов Айзък" },
                });
            }
            var reader = engine.CreateReader();
            var result = await reader.Search(new[] { "стихи", "пушкина" }, false);
            foreach (var pair in result)
            {
                Console.WriteLine($"[{pair.Key}]: {pair.Value}");
            }
            //await foreach (var fieldRecords in reader.GetAll())
            //{
            //    Console.WriteLine(fieldRecords.Field);
            //}
        }

        static async Task TestEngine()
        {
            ILexProvider lexProvider = new LexProvider(new RussianStemmer());

            var tempFolder = Path.Combine(Configuration.BaseDirectory, "SleoTestStorage");
            FSUtils.CleanAndTestFolder(tempFolder);

            var lex = new Func<string, string>(s => string.Join(" ", lexProvider.ExtractUniqueLexTokens(s).Select(s => s.Token)));

            var engine = new SleoEngine<BookDocument>(tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[]
                {
                    new BookDocument{ Id = "01", Title = "Юность Пушкина", Author = "Егорова Елена", Stemms = lex("Юность Пушкина") },
                    new BookDocument{ Id = "02", Title = "Детство Александра Пушкина", Author = "Егорова Елена Николаевна", Stemms = lex("Детство Александра Пушкина") },
                    new BookDocument{ Id = "03", Title = "Избранные стихи", Author = "Александра Пушкина", Stemms = lex("Избранные стихи") },
                    new BookDocument{ Id = "04", Title = "Анализ стихотворений Александра Сергеевича Пушкина", Author = "Ланцов Михаил", Stemms = lex("Анализ стихотворений Александра Сергеевича Пушкина") },

                    new BookDocument{ Id = "05", Title = "Море обаяния", Author = "Искандер Фазиль", Stemms = lex("Море обаяния") },
                    new BookDocument{ Id = "06", Title = "«Какаду»", Author = "Клысь Рышард", Stemms = lex("«Какаду»") },
                    new BookDocument{ Id = "07", Title = "Ряд случайных чисел [СИ]", Author = "Павлова Елена Евгеньевна", Stemms = lex("Ряд случайных чисел [СИ]") },
                    new BookDocument{ Id = "08", Title = "Последняя любовь. Плен и свобода", Author = "Мятная Витамина", Stemms = lex("Последняя любовь. Плен и свобода") },

                    new BookDocument{ Id = "09", Title = "Золотой ус. Лучшие рецепты исцеления", Author = "Альменов Чингиз", Stemms = lex("Золотой ус. Лучшие рецепты исцеления") },
                    new BookDocument{ Id = "10", Title = "Пушки смотрят на восток", Author = "Ефимова Марина Михайловна", Stemms = lex("Пушки смотрят на восто") },
                    new BookDocument{ Id = "11", Title = "Чингиз Хан, становление", Author = "Пушной Виталий", Stemms = lex("Чингиз Хан, становление") },
                });
            }

            var reader = engine.CreateReader();
            var result = await reader.Search(new[] { "Елена", "Евгеньевна" }, false);
            foreach (var pair in result)
            {
                Console.WriteLine($"[{pair.Key}]: {pair.Value}");
            }
        }

        static void TestCompression()
        {
            var one_zip = Compressor.Compress(new[] { "02" } );
            var one_unzip = Compressor.DecompressToDocuments(one_zip);


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

        static async Task TestSearch()
        {
            var tempFolder = Path.Combine(Configuration.BaseDirectory, "SleoTestStorage");
            FSUtils.CleanAndTestFolder(tempFolder);

            var store = new DataStorage(tempFolder);

            using (var writer = store.GetWriter("author"))
            {
                await writer.Write("Козлов Игорь", "1");
                await writer.Write("Ермакова Светлана Евгеньевна", "2");
                await writer.Write("Муркок Майкл   Лаумер Кейт   Пик Мервин   Ле Гуин Урсула   Дилэни Сэмюэль   Баллард Джеймс Грэм   Эллисон Харлан   Диксон Гордон   Нивен Ларри   Корнблат Сирил М   Вульф Джин   Лейбер Фриц Ройтер", "3");
                await writer.Write("Коллектив Авторов", "4");
                await writer.Write("Боннэр Елена Георгиевна", "5");
                await writer.Write("Звёздкина Анна  ", "6");
                await writer.Complete();
            }

            using (var writer = store.GetWriter("title"))
            {
                await writer.Write("Подкова на счастье", "1");
                await writer.Write("Среднеазиатская овчарка", "2");
                await writer.Write("Багряная игра. Сборник англо-американской фантастики", "3");
                await writer.Write("Управление проектами. Фундаментальный курс", "4");
                await writer.Write("Постскриптум: Книга о горьковской ссылке", "5");
                await writer.Write("Фарватер", "6");
                await writer.Complete();
            }



            var docs = await store.GetDocuments("title", new string[] { "Подкова на счастье" }, 1.0f, false);
            foreach (var kv in docs.OrderByDescending(kv => kv.Value))
            {
                Console.WriteLine($"[ID] = {kv.Key}: {kv.Value}");
            }
        }
    }
}
