﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZeroLevel.Services.HashFunctions;
using ZeroLevel.Services.PartitionStorage;

namespace ZeroLevel.Sleopok.Engine.Services.Storage
{
    public sealed class DataStorage
    {
        #region Private
        private class PositionDocScore
        {
            private float score = 0.0f;
            private int _last_position = -1;
            private int count = 0;

            public float GetScore(int total, bool exactMatch)
            {
                if (exactMatch)
                {
                    return (count == total) ? 1.0f : 0f;
                }
                return (score / (float)total) * count;
            }

            public void Increase(int position)
            {
                if (position == 0)
                {
                    score = 1.0f;
                }
                else
                {
                    var diff = position - _last_position;
                    score += 1.0f + 1.0f / diff;
                }
                _last_position = position;
                count++;
            }
        }

        private class DateSourceWriter :
            IPartitionDataWriter
        {
            private readonly IStorePartitionBuilder<string, string, byte[]> _builder;
            public DateSourceWriter(IStorePartitionBuilder<string, string, byte[]> builder)
            {
                _builder = builder;
            }

            public async Task Complete()
            {
                _builder.CompleteAdding();
                _builder.Compress();
                await _builder.RebuildIndex();
            }

            public async Task Write(string host, string document)
            {
                await _builder.Store(host, document);
            }

            public long GetTotalRecords() => _builder.TotalRecords;

            public void Dispose()
            {
                _builder.Dispose();
            }
        }
        #endregion

        private readonly IStore<string, string, byte[], StoreMetadata> _store;

        public DataStorage(string rootFolder)
        {
            var serializers = new StoreSerializers<string, string, byte[]>(
                async (w, n) => await w.WriteStringAsync(n),
                async (w, n) => await w.WriteStringAsync(n),
                async (w, n) => await w.WriteBytesAsync(n),

                async (r) => { try { return new DeserializeResult<string>(true, await r.ReadStringAsync()); } catch { return new DeserializeResult<string>(false, string.Empty); } },
                async (r) => { try { return new DeserializeResult<string>(true, await r.ReadStringAsync()); } catch { return new DeserializeResult<string>(false, string.Empty); } },
                async (r) => { try { return new DeserializeResult<byte[]>(true, await r.ReadBytesAsync()); } catch { return new DeserializeResult<byte[]>(false, new byte[0]); } });

            var options = new StoreOptions<string, string, byte[], StoreMetadata>
            {
                Index = new IndexOptions
                {
                    Enabled = true,
                    StepType = IndexStepType.Step,
                    StepValue = 32,
                    EnableIndexInMemoryCachee = false
                },
                RootFolder = rootFolder,
                FilePartition = new StoreFilePartition<string, StoreMetadata>("Token hash", (token, _) => Math.Abs(StringHash.DotNetFullHash(token.ToLowerInvariant()) % 47).ToString()),
                MergeFunction = list =>
                {
                    return Compressor.Compress(list.OrderBy(c => c).ToArray());
                },
                Partitions = new List<StoreCatalogPartition<StoreMetadata>>
                {
                    new StoreCatalogPartition<StoreMetadata>("Field", m => m.Field)
                },
                KeyComparer = (left, right) => string.Compare(left, right, true),
                ThreadSafeWriting = true
            };
            _store = new Store<string, string, byte[], StoreMetadata>(options, serializers);
        }

        public IPartitionDataWriter GetWriter(string field)
        {
            return new DateSourceWriter(_store.CreateBuilder(new StoreMetadata(field)));
        }

        /// <summary>
        /// Поиск документов.
        /// </summary>
        /// <param name="field">Поле по которому производится поиск.</param>
        /// <param name="tokens">Поисковый запрос.</param>
        /// <param name="boost">Множитель ранга для результата поиска.</param>
        /// <param name="exactMatch">true - если искать только точные совпадения.</param>
        /// <returns>Список идентификаторов документов с коэффициентом ранжирования.</returns>
        public async Task<Dictionary<string, float>> GetDocuments(string field, string[] tokens, float boost, bool exactMatch)
        {
            var documents = new Dictionary<string, PositionDocScore>();
            var accessor = _store.CreateAccessor(new StoreMetadata(field));
            if (accessor != null)
            {
                using (accessor)
                {
                    int step = 0;
                    foreach (var token in tokens)
                    {
                        var sr = await accessor.Find(token.ToLowerInvariant());
                        if (sr.Success)
                        {
                            foreach (var doc in Compressor.DecompressToDocuments(sr.Value))
                            {
                                if (false == documents.ContainsKey(doc))
                                {
                                    documents.Add(doc, new PositionDocScore());
                                }
                                documents[doc].Increase(step);
                            }
                        }
                        step++;
                    }
                }
            }
            return documents.ToDictionary(d => d.Key, d => boost * d.Value.GetScore(tokens.Length, exactMatch));
        }

        public async Task<Dictionary<string, List<string>>> GetAllDocuments(string field)
        {
            var documents = new Dictionary<string, List<string>>();
            var accessor = _store.CreateAccessor(new StoreMetadata(field));
            if (accessor != null)
            {
                using (accessor)
                {
                    await foreach (var data in accessor.Iterate())
                    {
                        data.Deconstruct(out string key, out byte[] val);
                        var docs = Compressor.DecompressToDocuments(val);
                        if (documents.TryGetValue(key, out var documentsIds))
                        {
                            documentsIds.AddRange(docs);
                        }
                        else
                        {
                            documents[key] = new List<string>(docs);
                        }
                    }
                }
            }
            return documents;
        }

        public async Task Dump(string key, Stream stream)
        {
            using (TextWriter writer = new StreamWriter(stream))
            {
                await foreach (var i in _store.Bypass(new StoreMetadata(key)))
                {
                    writer.WriteLine(i.Key);
                    writer.WriteLine(string.Join(' ', Compressor.DecompressToDocuments(i.Value)));
                }
            }
        }

        public int HasData(string field)
        {
            var partition = _store.CreateAccessor(new StoreMetadata(field));
            if (partition != null)
            {
                using (partition)
                {
                    return partition.CountDataFiles();
                }
            }
            return 0;
        }
    }
}
