# ZeroLevel

[![NuGet](https://img.shields.io/nuget/v/ZeroLevel.svg)](https://www.nuget.org/packages/ZeroLevel)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A self-contained `.NET Standard 2.1` toolkit that bundles the plumbing every non-trivial .NET service ends up writing: logging, configuration, DI, fast binary serialization, a duplex TCP protocol, a file-based partitioned key-value store, semantic helpers, scheduling, encryption, reflection helpers and more — all without pulling in a heavyweight framework.

> Designed as a drop-in foundation for background services, CLI tools and embedded engines. No code generation, no source generators, no reflection-heavy bootstrap — just references and go.

## Installation

```bash
dotnet add package ZeroLevel
```

Minimum target: `netstandard2.1` (compatible with .NET Core 3.0+, .NET 5+, Mono 6.4+, Xamarin).

External dependencies: `System.Text.Json`, `YamlDotNet`, `System.Runtime.CompilerServices.Unsafe`.

## What's inside

### Application basics

| Area | Highlights |
|---|---|
| **Logging** | `Log` static facade (`Log.Info`, `Log.Error`, `Log.Fatal`), pluggable `ILogger` backends, batched async log router. |
| **Configuration** | `Configuration` static facade, YAML/JSON readers, layered sets (`IConfigurationSet`), environment overrides. |
| **Dependency Injection** | Named containers, constructor injection, singleton/transient, `[Resolve]` / `[Parameter]` attributes, `Injector.Default` fast path. |
| **Scheduling** | Single-threaded fiber-based scheduler, cron-like triggers, async task queues. |
| **Services** | `BaseZeroService` + `Bootstrap` lifecycle for long-running processes. |

### Serialization

- `MessageSerializer` — compact binary serializer with compiled-expression codecs for ~50 primitive and nullable types, arrays, collections and dictionaries.
- Explicit `IBinarySerializable` / `IAsyncBinarySerializable` for hand-tuned layouts.
- `SerializeCompatible<T>` / `DeserializeCompatible<T>` — version-tolerant round-trips between layout variants.
- Lazy collection readers (`ReadCollectionLazy<T>`) that stream elements as they arrive.

### Networking

- Duplex TCP protocol with message / request-response / keep-alive framing.
- `SocketServer` + `SocketClient` built on explicit threads, `FrameParser` state machine, `RequestBuffer` for callback correlation.
- Adaptive buffer manager, chunked file transfer (`FileTransfer/`).
- Service discovery client (`IDiscoveryClient`) and pluggable routing (`IRouter` / `IServiceRoutesStorage`).

### Storage and data

- **PartitionStorage** — embedded key-value store partitioned by computed meta (date, tenant, hash), with merge steps, sparse indexes and compression hooks.
- **DataStructures** — `BloomFilter`, `HyperBloomBloom`, `BitMapCardTable`, `SafeBit32Vector`, `SparceVector`, `SparseMatrix`.

### Semantic and text

- Snowball stemmer adapter, stop-word lists, n-gram tokenizer, edit-distance metrics.
- `TextAnalizer.ExtractWords` / `ExtractRuWords` (Russian-aware regex), `WordToken` with positions.
- Transliteration, text-on-curve rendering, plain-text table renderer.

### Utilities

- Murmur3 hashing, Rijndael encryption, `TokenEncryptor`, deterministic `StringHash`.
- `FSUtils` (path correction, folder archiving), leak-free `FileSystemWatcher` wrapper.
- `InvokeWrapper` — cached compiled delegates by `name + signature`.
- Object mapping (`ObjectMapping`), PredicateBuilder for dynamic LINQ, Specification pattern, Query pipeline.
- Tries, suffix automata, priority queues, round-robin / sparse iterators, `EverythingStorage`, `FixSizeQueue`, `AtomicBoolean`.

## Quick start

### Logging

```csharp
using ZeroLevel;

Log.AddConsoleLogger(LogLevel.Info | LogLevel.Error);
Log.Info("Service {0} started", "indexer");
```

### Configuration

```csharp
var cfg = Configuration.ReadFromApplicationConfig();
var port = cfg.First<int>("port");
```

### Dependency Injection

```csharp
using ZeroLevel;

Injector.Default.Register<ILogger, ConsoleLogger>();
Injector.Default.Register<IMyService, MyService>();

var svc = Injector.Default.Resolve<IMyService>();
```

### Binary serialization

```csharp
using ZeroLevel.Services.Serialization;

public sealed class Payload : IBinarySerializable
{
    public string Title;
    public DateTimeOffset CreatedAt;

    public void Serialize(IBinaryWriter w)   { w.WriteString(Title);  w.WriteDateTimeOffset(CreatedAt); }
    public void Deserialize(IBinaryReader r) { Title = r.ReadString(); CreatedAt = r.ReadDateTimeOffset(); }
}

byte[] bytes = MessageSerializer.Serialize(new Payload { Title = "hi", CreatedAt = DateTimeOffset.UtcNow });
Payload back = MessageSerializer.Deserialize<Payload>(bytes);
```

### Duplex TCP client

```csharp
var exchange = UseExchange();
var client = exchange.GetConnection("127.0.0.1:3456");
client.Send("ping");
var pong = await client.Request<PingRequest, PongResponse>(new PingRequest());
```

## Source layout

```
ZeroLevel/
├── DataStructures/        BloomFilter, SparseMatrix, BitMapCardTable, ...
├── Models/                BaseModel, InvokeResult, ZeroServiceInfo, DataRequest
└── Services/
    ├── Cache/             TimerCache and friends
    ├── Collections/       EverythingStorage, FixSizeQueue, RoundRobinCollection, ...
    ├── Config/            Configuration facade, YAML/JSON loaders
    ├── DOM/               DSL for templated text generation
    ├── DependencyInjection/   Injector facade, Container, attributes
    ├── Drawing/           Text-on-curve rendering
    ├── Encryption/        Rijndael, token encryption
    ├── FileSystem/        FSUtils, ParallelFileReader, archive helpers
    ├── Formats/           YAML ↔ JSON converter
    ├── HashFunctions/     Murmur3, StringHash (deterministic)
    ├── Invokation/        Cached compiled delegates
    ├── Logging/           Log facade, routers, buffered loggers
    ├── Mathemathics/      (sic — keep the legacy spelling)
    ├── Memory/            MMF view accessors
    ├── Network/           SocketServer / SocketClient / Exchange / FrameParser
    ├── ObjectMapping/     Property-level mapping
    ├── PartitionStorage/  File-based partitioned KV store
    ├── Queries/           PredicateBuilder, query pipeline
    ├── Reflection/        TypeHelpers, getter/setter builders
    ├── Semantic/          Stemmers, stop-words, n-grams, distances
    ├── Serialization/     MessageSerializer, PrimitiveTypeSerializer
    ├── Shedulling/        (sic) Fiber-based scheduler
    ├── Specification/     Specification pattern
    ├── Text/              Transliteration, plain-text tables
    ├── Trees/             Trie, suffix automata
    └── Web/, Windows/, Utils/, Pools/, MemoryPools/, Extensions/
```

## Notes

- Namespace `Mathemathics` is an intentional legacy spelling; renaming it would break downstream users and is explicitly avoided.
- `netstandard2.1` constrains some of the newer BCL APIs; the library stays within that surface on purpose.
- Logging, Configuration, Bootstrap and Injector are exposed as **static facades** (e.g. `Log.Info("msg")`) so that startup boilerplate is minimal; all have imperative APIs as well.

## License

MIT © [Ogoun](https://github.com/ogoun). See [LICENSE](https://github.com/ogoun/Zero/blob/master/LICENSE) for details.
