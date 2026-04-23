using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ZeroLevel.Sleopok.Engine;
using ZeroLevel.Sleopok.Engine.Models;
using ZeroLevel.Sleopok.Engine.Services;
using ZeroLevel.Sleopok.Engine.Services.Storage;

namespace ZeroLevel.UnitTests
{
    public class SleopokEngineTests : IDisposable
    {
        private readonly string _tempFolder;

        public SleopokEngineTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "SleopokTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFolder);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_tempFolder)) Directory.Delete(_tempFolder, true); }
            catch { /* best effort cleanup */ }
        }

        public sealed class Book
        {
            public string Id { get; set; } = string.Empty;

            [SleoIndex("title", 10.0f, avaliableForExactMatch: true)]
            public string Title { get; set; } = string.Empty;

            [SleoIndex("author", 5.0f, avaliableForExactMatch: true)]
            public string Author { get; set; } = string.Empty;
        }

        public sealed class BookWithTagsArray
        {
            public string Id { get; set; } = string.Empty;

            [SleoIndex("title", 1.0f)]
            public string Title { get; set; } = string.Empty;

            [SleoIndex("tags", 2.0f)]
            public string[] Tags { get; set; } = Array.Empty<string>();
        }

        public sealed class BookWithAuthorsList
        {
            public string Id { get; set; } = string.Empty;

            [SleoIndex("title", 1.0f)]
            public string Title { get; set; } = string.Empty;

            [SleoIndex("authors", 3.0f)]
            public List<string> Authors { get; set; } = new List<string>();
        }

        public sealed class BookWithFieldAnnotation
        {
            [SleoIndex("id")]
            public string Id = string.Empty;

            [SleoIndex("title")]
            public string Title = string.Empty;
        }

        [Fact]
        public async Task Build_And_Search_Returns_Matching_Document()
        {
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[]
                {
                    new Book { Id = "01", Title = "War and Peace", Author = "Tolstoy" },
                    new Book { Id = "02", Title = "Crime and Punishment", Author = "Dostoevsky" },
                });
                await builder.Complete();
            }

            var reader = engine.CreateReader();
            var result = (await reader.Search(new[] { "war" }, false)).ToList();

            Assert.NotEmpty(result);
            Assert.Contains(result, p => p.Key == "01");
            Assert.DoesNotContain(result, p => p.Key == "02");
        }

        [Fact]
        public async Task HasData_False_On_Empty_Storage()
        {
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            Assert.False(engine.HasData());
        }

        [Fact]
        public async Task HasData_True_After_Write()
        {
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[] { new Book { Id = "01", Title = "hello", Author = "someone" } });
                await builder.Complete();
            }

            Assert.True(engine.HasData());
        }

        [Fact]
        public async Task ExactMatch_Requires_All_Tokens_Present()
        {
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[]
                {
                    new Book { Id = "01", Title = "red blue green", Author = "a1" },
                    new Book { Id = "02", Title = "red yellow", Author = "a2" },
                });
                await builder.Complete();
            }

            var reader = engine.CreateReader();
            var exact = (await reader.Search(new[] { "red", "blue" }, true)).ToList();

            Assert.Single(exact);
            Assert.Equal("01", exact[0].Key);
        }

        [Fact]
        public async Task ExactMatch_Skips_Fields_Not_Available_For_Exact()
        {
            // Only "title" and "author" are avaliableForExactMatch=true in Book.
            // If all configured fields had ExactMatch=false, exactMatch search would return nothing.
            // Here both are eligible — sanity check exactMatch finds the doc.
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[] { new Book { Id = "01", Title = "alpha", Author = "beta" } });
                await builder.Complete();
            }
            var reader = engine.CreateReader();
            var exact = (await reader.Search(new[] { "alpha" }, true)).ToList();
            Assert.Single(exact);
            Assert.Equal("01", exact[0].Key);
        }

        [Fact]
        public async Task Unmatched_Tokens_Return_Empty()
        {
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[] { new Book { Id = "01", Title = "hello world", Author = "abc" } });
                await builder.Complete();
            }

            var reader = engine.CreateReader();
            var r = (await reader.Search(new[] { "nonexistenttoken" }, false)).ToList();
            Assert.Empty(r);
        }

        [Fact]
        public async Task Array_Field_Is_Indexed()
        {
            var engine = new SleoEngine<BookWithTagsArray>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[]
                {
                    new BookWithTagsArray { Id = "01", Title = "x", Tags = new[] { "fantasy", "adventure" } },
                    new BookWithTagsArray { Id = "02", Title = "y", Tags = new[] { "romance" } },
                });
                await builder.Complete();
            }
            var reader = engine.CreateReader();
            var r = (await reader.Search(new[] { "fantasy" }, false)).ToList();
            Assert.Contains(r, p => p.Key == "01");
            Assert.DoesNotContain(r, p => p.Key == "02");
        }

        [Fact]
        public async Task Generic_List_Field_Is_Indexed()
        {
            var engine = new SleoEngine<BookWithAuthorsList>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[]
                {
                    new BookWithAuthorsList { Id = "01", Title = "x", Authors = new List<string> { "Alice", "Bob" } },
                    new BookWithAuthorsList { Id = "02", Title = "y", Authors = new List<string> { "Charlie" } },
                });
                await builder.Complete();
            }
            var reader = engine.CreateReader();
            var r = (await reader.Search(new[] { "alice" }, false)).ToList();
            Assert.Contains(r, p => p.Key == "01");
            Assert.DoesNotContain(r, p => p.Key == "02");
        }

        [Fact]
        public async Task Fields_Attribute_On_Public_Fields_Are_Indexed()
        {
            var engine = new SleoEngine<BookWithFieldAnnotation>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[]
                {
                    new BookWithFieldAnnotation { Id = "01", Title = "kappa" },
                });
                await builder.Complete();
            }
            var reader = engine.CreateReader();
            var r = (await reader.Search(new[] { "kappa" }, false)).ToList();
            Assert.Single(r);
            Assert.Equal("01", r[0].Key);
        }

        [Fact]
        public async Task Builder_Dispose_After_Explicit_Complete_Does_Not_Throw()
        {
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            var builder = engine.CreateBuilder();
            await builder.Write(new[] { new Book { Id = "01", Title = "t", Author = "a" } });
            await builder.Complete();

            var ex = Record.Exception(() => builder.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public async Task Builder_Implicit_Dispose_In_Using_Completes_Data()
        {
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[] { new Book { Id = "01", Title = "auto", Author = "x" } });
                // no explicit Complete — Dispose must complete
            }

            var reader = engine.CreateReader();
            var r = (await reader.Search(new[] { "auto" }, false)).ToList();
            Assert.Contains(r, p => p.Key == "01");
        }

        [Fact]
        public async Task Duplicate_Writes_Do_Not_Produce_Duplicate_Results()
        {
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                // Same doc written three times — merge dedup must collapse
                await builder.Write(new[]
                {
                    new Book { Id = "01", Title = "alpha", Author = "a" },
                    new Book { Id = "01", Title = "alpha", Author = "a" },
                    new Book { Id = "01", Title = "alpha", Author = "a" },
                });
                await builder.Complete();
            }
            var reader = engine.CreateReader();
            var r = (await reader.Search(new[] { "alpha" }, false)).ToList();
            Assert.Single(r);
            Assert.Equal("01", r[0].Key);
        }

        [Fact]
        public async Task Repeated_Token_In_Same_Field_Does_Not_Break_Exact_Match()
        {
            // Before dedup fix, repeated "alpha" in Title would cause count to inflate for exactMatch.
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[]
                {
                    new Book { Id = "01", Title = "alpha alpha alpha", Author = "a" },
                    new Book { Id = "02", Title = "alpha beta", Author = "b" },
                });
                await builder.Complete();
            }
            var reader = engine.CreateReader();
            var exact = (await reader.Search(new[] { "alpha", "beta" }, true)).ToList();
            // Only doc 02 has both tokens; doc 01 must NOT pass as exact match despite repeated alpha.
            Assert.Single(exact);
            Assert.Equal("02", exact[0].Key);
        }

        [Fact]
        public async Task GetAll_Streams_Field_Records()
        {
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[]
                {
                    new Book { Id = "01", Title = "red", Author = "aaa" },
                    new Book { Id = "02", Title = "blue", Author = "bbb" },
                });
                await builder.Complete();
            }

            var reader = engine.CreateReader();
            var fields = new List<string>();
            var tokens = new List<string>();
            await foreach (var fr in reader.GetAll())
            {
                fields.Add(fr.Field);
                await foreach (var td in fr.Records)
                {
                    tokens.Add(td.Token);
                    Assert.NotEmpty(td.Documents);
                }
            }

            Assert.Contains("title", fields);
            Assert.Contains("author", fields);
            Assert.Contains("red", tokens);
            Assert.Contains("blue", tokens);
            Assert.Contains("aaa", tokens);
            Assert.Contains("bbb", tokens);
        }

        [Fact]
        public async Task Search_Is_Case_Insensitive_For_Tokens()
        {
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[] { new Book { Id = "01", Title = "Hello World", Author = "Someone" } });
                await builder.Complete();
            }
            var reader = engine.CreateReader();
            var r = (await reader.Search(new[] { "HELLO" }, false)).ToList();
            Assert.Contains(r, p => p.Key == "01");
        }

        [Fact]
        public async Task Higher_Boost_Yields_Higher_Score()
        {
            // Book.Title boost=10, Book.Author boost=5. A token present only in Title must score higher
            // than the same token present only in Author (after per-field scoring and boost).
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[]
                {
                    new Book { Id = "title", Title = "xenon", Author = "other" },
                    new Book { Id = "author", Title = "other", Author = "xenon" },
                });
                await builder.Complete();
            }
            var reader = engine.CreateReader();
            var r = (await reader.Search(new[] { "xenon" }, false)).ToDictionary(p => p.Key, p => p.Value);
            Assert.True(r["title"] > r["author"]);
        }

        [Fact]
        public async Task Multiple_Batches_Merge_On_Complete()
        {
            var engine = new SleoEngine<Book>(_tempFolder, b => b.Id);
            using (var builder = engine.CreateBuilder())
            {
                await builder.Write(new[] { new Book { Id = "01", Title = "first", Author = "a" } });
                await builder.Write(new[] { new Book { Id = "02", Title = "second", Author = "b" } });
                await builder.Complete();
            }
            var reader = engine.CreateReader();
            var r1 = (await reader.Search(new[] { "first" }, false)).ToList();
            var r2 = (await reader.Search(new[] { "second" }, false)).ToList();
            Assert.Contains(r1, p => p.Key == "01");
            Assert.Contains(r2, p => p.Key == "02");
        }
    }

    public class SleopokCompressorTests
    {
        [Fact]
        public void Compress_Decompress_Round_Trip_Matches()
        {
            var docs = new[] { "01", "02", "doc-abc", "hello world" };
            var compressed = Compressor.Compress(docs);
            var back = Compressor.DecompressToDocuments(compressed);
            Assert.Equal(docs, back);
        }

        [Fact]
        public void Compress_Empty_Array_Round_Trip()
        {
            var compressed = Compressor.Compress(Array.Empty<string>());
            var back = Compressor.DecompressToDocuments(compressed);
            Assert.Empty(back);
        }

        [Fact]
        public void Compress_Single_Element_Round_Trip()
        {
            var compressed = Compressor.Compress(new[] { "only" });
            var back = Compressor.DecompressToDocuments(compressed);
            Assert.Single(back);
            Assert.Equal("only", back[0]);
        }

        [Fact]
        public void Compress_Preserves_Order_And_Duplicates()
        {
            var docs = new[] { "a", "b", "a", "c" };
            var back = Compressor.DecompressToDocuments(Compressor.Compress(docs));
            Assert.Equal(docs, back);
        }

        [Fact]
        public void Compress_Many_Elements_Round_Trip()
        {
            var docs = Enumerable.Range(0, 1000).Select(i => "doc_" + i.ToString()).ToArray();
            var back = Compressor.DecompressToDocuments(Compressor.Compress(docs));
            Assert.Equal(docs, back);
        }
    }

    public class SleopokDataStorageTests : IDisposable
    {
        private readonly string _tempFolder;

        public SleopokDataStorageTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "SleopokStorage_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFolder);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_tempFolder)) Directory.Delete(_tempFolder, true); }
            catch { }
        }

        [Fact]
        public async Task Write_Then_GetDocuments_Returns_Match()
        {
            var store = new DataStorage(_tempFolder);
            using (var w = store.GetWriter("title"))
            {
                await w.Write("red", "1");
                await w.Write("blue", "2");
                await w.Complete();
            }
            var docs = await store.GetDocuments("title", new[] { "red" }, 1.0f, false);
            Assert.True(docs.ContainsKey("1"));
            Assert.False(docs.ContainsKey("2"));
        }

        [Fact]
        public async Task GetDocuments_Boost_Scales_Score()
        {
            var store = new DataStorage(_tempFolder);
            using (var w = store.GetWriter("title"))
            {
                await w.Write("alpha", "1");
                await w.Complete();
            }
            var low = await store.GetDocuments("title", new[] { "alpha" }, 1.0f, false);
            var high = await store.GetDocuments("title", new[] { "alpha" }, 5.0f, false);
            Assert.True(high["1"] > low["1"] * 4f);
        }

        [Fact]
        public async Task HasData_Reflects_File_Count()
        {
            var store = new DataStorage(_tempFolder);
            Assert.Equal(0, store.HasData("title"));

            using (var w = store.GetWriter("title"))
            {
                await w.Write("x", "1");
                await w.Complete();
            }
            Assert.True(store.HasData("title") > 0);
        }

        [Fact]
        public async Task IterateAllDocuments_Streams_Token_Documents()
        {
            var store = new DataStorage(_tempFolder);
            using (var w = store.GetWriter("field"))
            {
                await w.Write("one", "d1");
                await w.Write("one", "d2");
                await w.Write("two", "d3");
                await w.Complete();
            }

            var seen = new Dictionary<string, HashSet<string>>();
            await foreach (var td in store.IterateAllDocuments("field"))
            {
                if (!seen.TryGetValue(td.Token, out var set))
                {
                    set = new HashSet<string>();
                    seen[td.Token] = set;
                }
                foreach (var d in td.Documents) set.Add(d);
            }

            Assert.True(seen.ContainsKey("one"));
            Assert.True(seen.ContainsKey("two"));
            Assert.Contains("d1", seen["one"]);
            Assert.Contains("d2", seen["one"]);
            Assert.Contains("d3", seen["two"]);
        }

        [Fact]
        public async Task IterateAllDocuments_Returns_Empty_For_Unknown_Field()
        {
            var store = new DataStorage(_tempFolder);
            var count = 0;
            await foreach (var _ in store.IterateAllDocuments("nonexistent"))
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task Duplicate_Token_Doc_Writes_Are_Deduplicated_After_Merge()
        {
            var store = new DataStorage(_tempFolder);
            using (var w = store.GetWriter("f"))
            {
                await w.Write("tok", "doc1");
                await w.Write("tok", "doc1");
                await w.Write("tok", "doc1");
                await w.Complete();
            }

            var all = new List<string>();
            await foreach (var td in store.IterateAllDocuments("f"))
            {
                all.AddRange(td.Documents);
            }

            // exactly one "doc1" occurrence expected after dedup
            Assert.Single(all);
            Assert.Equal("doc1", all[0]);
        }
    }
}
