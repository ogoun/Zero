using System;
using System.Collections.Generic;
using ZeroLevel.HNSW;

namespace HNSWDemo.Model
{
    public class Person
    {
        public Gender Gender { get; set; }
        public int Age { get; set; }
        public long Number { get; set; }

        private static (float[], Person) Generate(int vector_size)
        {
            var rnd = new Random((int)Environment.TickCount);
            var vector = new float[vector_size];
            DefaultRandomGenerator.Instance.NextFloats(vector);
            VectorUtils.NormalizeSIMD(vector);
            var p = new Person();
            p.Age = rnd.Next(15, 80);
            var gr = rnd.Next(0, 3);
            p.Gender = (gr == 0) ? Gender.Male : (gr == 1) ? Gender.Feemale : Gender.Unknown;
            p.Number = CreateNumber(rnd);
            return (vector, p);
        }

        public static List<(float[], Person)> GenerateRandom(int vectorSize, int vectorsCount)
        {
            var vectors = new List<(float[], Person)>();
            for (int i = 0; i < vectorsCount; i++)
            {
                vectors.Add(Generate(vectorSize));
            }
            return vectors;
        }

        static HashSet<long> _exists = new HashSet<long>();
        private static long CreateNumber(Random rnd)
        {
            long start_number;
            do
            {
                start_number = 79600000000L;
                start_number = start_number + rnd.Next(4, 8) * 10000000;
                start_number += rnd.Next(0, 1000000);
            }
            while (_exists.Add(start_number) == false);
            return start_number;
        }
    }
}
