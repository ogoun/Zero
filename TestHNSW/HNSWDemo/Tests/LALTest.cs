﻿using HNSWDemo.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel.HNSW;
using ZeroLevel.Services.Mathemathics;

namespace HNSWDemo.Tests
{
    internal class LALTest
        : ITest
    {
        private const int count = 20000;
        private const int dimensionality = 128;
        private const string _graphFileCachee = @"lal_test_graph.bin";
        private const string _mapFileCachee = @"lal_test_map.bin";

        public void Run()
        {
            var moda = 3;
            var persons = Person.GenerateRandom(dimensionality, count);
            var samples = new Dictionary<int, List<(float[], Person)>>();
            var options = NSWOptions<float[]>.Create(6, 8, 100, 100, Metrics.CosineDistance);

            foreach (var p in persons)
            {
                var c = (int)Math.Abs(p.Item2.Number.GetHashCode() % moda);
                if (samples.ContainsKey(c) == false) samples.Add(c, new List<(float[], Person)>());
                samples[c].Add(p);
            }

            SplittedLALGraph worlds;
            HNSWMappers<long> mappers;

            if (File.Exists(_graphFileCachee) && File.Exists(_mapFileCachee))
            {
                worlds = new SplittedLALGraph(_graphFileCachee);
                mappers = new HNSWMappers<long>(_mapFileCachee, l => (int)Math.Abs(l.GetHashCode() % moda));
            }
            else
            {

                worlds = new SplittedLALGraph();
                mappers = new HNSWMappers<long>(l => (int)Math.Abs(l.GetHashCode() % moda));

                var worlds_dict = new Dictionary<int, SmallWorld<float[]>>();
                var maps_dict = new Dictionary<int, HNSWMap<long>>();

                foreach (var p in samples)
                {
                    var c = p.Key;
                    if (worlds_dict.ContainsKey(c) == false)
                    {
                        worlds_dict.Add(c, new SmallWorld<float[]>(options));
                    }
                    if (maps_dict.ContainsKey(c) == false)
                    {
                        maps_dict.Add(c, new HNSWMap<long>());
                    }
                    var w = worlds_dict[c];
                    var m = maps_dict[c];
                    var ids = w.AddItems(p.Value.Select(i => i.Item1));

                    for (int i = 0; i < ids.Length; i++)
                    {
                        m.Append(p.Value[i].Item2.Number, ids[i]);
                    }
                }

                var name = Guid.NewGuid().ToString();
                foreach (var p in samples)
                {
                    var c = p.Key;
                    var w = worlds_dict[c];
                    var m = maps_dict[c];

                    using (var s = File.Create(name))
                    {
                        w.Serialize(s);
                    }
                    using (var s = File.OpenRead(name))
                    {
                        var l = LALGraph.FromHNSWGraph<float[]>(s);
                        worlds.Append(l, c);
                    }
                    File.Delete(name);
                    mappers.Append(m, c);
                }

                worlds.Save(_graphFileCachee);
                mappers.Save(_mapFileCachee);
            }

            var entries = new long[10];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = persons[DefaultRandomGenerator.Instance.Next(0, persons.Count - 1)].Item2.Number;
            }
            var contexts = mappers.CreateContext(null, entries);
            var result = worlds.KNearest(5000, contexts);

            Console.WriteLine($"Found: {result.Sum(r=>r.Value.Count)}");

            /*Console.WriteLine("Entries:");
            foreach (var n in entries)
            {
                Console.WriteLine($"\t{n}");
            }

            Console.WriteLine("Extensions:");
            foreach (var r in result)
            {
                foreach (var n in mappers.ConvertIdsToFeatures(r.Key, r.Value))
                {
                    Console.WriteLine($"\t[{n}]");
                }
            }*/
        }
    }
}