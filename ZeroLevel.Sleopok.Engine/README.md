# ZeroLevel.Sleopok.Engine

[![NuGet](https://img.shields.io/nuget/v/ZeroLevel.Sleopok.Engine.svg)](https://www.nuget.org/packages/ZeroLevel.Sleopok.Engine)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

An **embeddable inverted-index search engine** for .NET — a simplified, in-process alternative to Elasticsearch/Lucene when you just need fast full-text lookup over a local dataset without running a separate service.

Sleopok is built on top of the [`ZeroLevel`](https://www.nuget.org/packages/ZeroLevel) toolkit and its `PartitionStorage` — so the index itself is just a set of compressed files on disk. No daemons, no JVM, no network hop.

## Why Sleopok

- **Embedded.** Runs in-process; the index is a folder you control.
- **Attribute-driven.** Mark the fields you want indexed with `[SleoIndex]` — no schema files, no DSL.
- **Relevance out of the box.** Per-field `Boost`, token-position scoring, optional exact-match mode.
- **Collections supported.** `string[]`, `List<string>` and any `IEnumerable<string>` are indexed element-by-element.
- **Streaming read-back.** `GetAll()` streams every `(field, token, documents)` triple without materializing the index in memory.
- **Compressed posting lists.** Deduplicated and GZip-compressed at merge time.
- **Stable and predictable.** No external runtime; targets `netstandard2.1`.

## Installation

```bash
dotnet add package ZeroLevel.Sleopok.Engine
```

`ZeroLevel.Sleopok.Engine` transitively pulls in [`ZeroLevel`](https://www.nuget.org/packages/ZeroLevel) of the matching version.

## Quick start

### 1. Describe your document

```csharp
using ZeroLevel.Sleopok.Engine.Models;

public sealed class Book
{
    public string Id { get; set; }

    [SleoIndex("title", boost: 10.0f, avaliableForExactMatch: true)]
    public string Title { get; set; }

    [SleoIndex("author", boost: 5.0f, avaliableForExactMatch: true)]
    public string Author { get; set; }

    [SleoIndex("tags", boost: 2.0f)]
    public string[] Tags { get; set; }
}
```

`Id` is whatever identifies a document; a `Func<T, string>` extractor is given to the engine at construction time.

### 2. Build the index

```csharp
using ZeroLevel.Sleopok.Engine;

var engine = new SleoEngine<Book>(indexFolder: "./index", identityExtractor: b => b.Id);

using (var builder = engine.CreateBuilder())
{
    await builder.Write(new[]
    {
        new Book { Id = "01", Title = "War and Peace",        Author = "Tolstoy",    Tags = new[] { "classic", "novel" } },
        new Book { Id = "02", Title = "Crime and Punishment", Author = "Dostoevsky", Tags = new[] { "classic" } },
        new Book { Id = "03", Title = "Hyperion",             Author = "Dan Simmons", Tags = new[] { "sci-fi", "novel" } },
    });
    await builder.Complete();   // flushes, merges and dedups posting lists
}
```

You can call `Write` multiple times in a single builder session — everything is merged on `Complete`.

### 3. Query

```csharp
var reader = engine.CreateReader();

// ranked fuzzy match — any subset of tokens is accepted, higher overlap + token proximity wins
var ranked = await reader.Search(new[] { "war", "peace" }, exactMatch: false);
foreach (var pair in ranked)
{
    Console.WriteLine($"[{pair.Key}] score={pair.Value:F2}");
}

// strict mode — only docs that contain *all* tokens in at least one exact-match-eligible field
var exact = await reader.Search(new[] { "war", "peace" }, exactMatch: true);
```

### 4. Stream the full index (diagnostics, re-indexing, export)

```csharp
await foreach (var field in reader.GetAll())
{
    Console.WriteLine($"Field: {field.Field}");
    await foreach (var entry in field.Records)
    {
        Console.WriteLine($"  token={entry.Token}  docs=[{string.Join(",", entry.Documents)}]");
    }
}
```

`GetAll()` never loads a whole field into memory — tokens are yielded as they are read from disk.

## How it works

1. **Tokenization.** For each indexed field, values are split by `TextAnalizer.ExtractWords` (a Unicode-aware regex that understands Cyrillic) and lowercased.
2. **Partitioning.** Tokens are bucketed by `StringHash.DotNetFullHash(token) % 47` — each bucket becomes one partition file per field, so writes and reads scale across tokens without loading everything.
3. **Posting lists.** Each `(token → [doc_id, ...])` entry is deduplicated on `Complete()` and GZip-compressed.
4. **Scoring.** At query time, `PositionDocScore` combines (a) how many query tokens a document matches, (b) how close their positions are, and (c) the per-field `Boost` value. Exact-match mode bypasses proximity and simply checks that all tokens appear for at least one eligible field.

## Public API

| Type | Purpose |
|---|---|
| `SleoEngine<T>(string indexFolder, Func<T, string> identityExtractor)` | Entry point. Owns the index folder. |
| `SleoEngine<T>.HasData()` | `true` if any partition file exists. |
| `SleoEngine<T>.CreateBuilder()` → `IIndexBuilder<T>` | Batch writer. `Write`, `Complete`, `Dispose`. |
| `SleoEngine<T>.CreateReader()` → `IIndexReader<T>` | `Search(string[] tokens, bool exactMatch)`, `GetAll()`. |
| `[SleoIndex(name, boost, avaliableForExactMatch)]` | Marks a field/property as indexable. |
| `DataStorage` | Lower-level public API (raw writes, `IterateAllDocuments`, `HasData`). |
| `TokenDocuments(string Token, string[] Documents)` | One entry in a streaming read. |
| `Compressor.Compress(string[]) / DecompressToDocuments(byte[])` | The posting-list codec, exposed for tooling. |

## FAQ

**Does it support deletes / updates?** Not directly — the index is append-only within a build session. Rebuild when you need to reflect deletions. Duplicate writes of the same `(token, doc_id)` collapse automatically on merge.

**Are writes thread-safe?** `IIndexBuilder<T>.Write` is not meant to be called concurrently for the same builder. Use one builder per ingestion task; the underlying `PartitionStorage` is `ThreadSafeWriting = true`.

**Case sensitivity?** Queries and indexed text are normalized via `ToLowerInvariant()` — `"Hello"` matches `"HELLO"` and `"hello"`.

**Stop-words / stemming?** Not built in. Pre-process your text yourself (for example with the `Iveonik.Stemmers` packages) and feed cleaned tokens into an extra `[SleoIndex]` field.

**Collection fields as one token vs. tokenized?** Each element of a collection is passed through the same tokenizer as a single value. So `Tags = new[] { "sci-fi", "novel" }` contributes three tokens: `sci`, `fi`, `novel`.

## License

MIT © [Ogoun](https://github.com/ogoun). See the repository [LICENSE](https://github.com/ogoun/Zero/blob/master/LICENSE).
