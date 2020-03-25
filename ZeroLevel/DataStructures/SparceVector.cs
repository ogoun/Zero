using System;
using System.Collections.Generic;
using ZeroLevel.Models;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DataStructures
{
    public sealed class SparceVector
            : IBinarySerializable
    {
        private readonly static int[] EmptyIndexes = new int[0];
        private readonly static double[] EmptyValues = new double[0];

        private int[] indexes;
        private double[] values;
        private double power;



        public SparceVector() 
        {
            indexes = EmptyIndexes;
            values = EmptyValues;
            power = 0;
        }

        public SparceVector(double[] vector)
        {
            var l = new List<int>();
            for (int i = 0; i < vector.Length; i++)
            {
                if (Math.Abs(vector[i]) > double.Epsilon)
                {
                    l.Add(i);
                }
            }
            indexes = l.ToArray();
            values = new double[l.Count];
            power = 0;
            for (int i = 0; i < l.Count; i++)
            {
                values[i] = vector[indexes[i]];
                power += values[i] * values[i];
            }
            power = Math.Sqrt(power);
        }

        public SparceVector(double[] vector, int[] indicies)
        {
            indexes = indicies;
            values = vector;
            power = 0;
            for (int i = 0; i < indexes.Length; i++)
            {
                power += values[i] * values[i];
            }
            power = Math.Sqrt(power);
        }

        public double Measure(SparceVector other)
        {
            double sum = 0.0d;

            int li = 0, ri = 0;
            int lv, rv;

            while (li < this.indexes.Length &&
                ri < other.indexes.Length)
            {
                lv = this.indexes[li];
                rv = other.indexes[ri];
                if (lv == rv)
                {
                    // у обоих векторов совпадение по индексам
                    sum += this.values[li] * other.values[ri];
                    li++; ri++;
                }
                else if (lv < rv)
                {
                    li++;
                }
                else
                {
                    ri++;
                }
            }
            return sum / (this.power * other.power);
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteDouble(this.power);
            writer.WriteCollection(indexes);
            writer.WriteCollection(values);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.power = reader.ReadDouble();
            this.indexes = reader.ReadInt32Collection().ToArray();
            this.values = reader.ReadDoubleCollection().ToArray();
        }
    }

    public sealed class SparseVector<T>
        where T : IEmptyable
    {
        private readonly static int[] EmptyIndexes = new int[0];
        private readonly static T[] EmptyValues = new T[0];

        private int[] indexes;
        private T[] values;

        public int Count => indexes?.Length ?? 0;

        public SparseVector()
        {
            indexes = EmptyIndexes;
            values = EmptyValues;
        }

        public SparseVector(T[] vector)
        {
            var l = new List<int>();
            for (int i = 0; i < vector.Length; i++)
            {
                if (!vector[i].IsEmpty)
                {
                    l.Add(i);
                }
            }
            indexes = l.ToArray();
            values = new T[l.Count];
            for (int i = 0; i < l.Count; i++)
            {
                values[i] = vector[indexes[i]];
            }
        }

        public SparseVector(T[] vector, int[] indicies)
        {
            indexes = indicies;
            values = vector;
        }

        public (T, int) this[int index] => (values[index], indexes[index]);
    }
}
