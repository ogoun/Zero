using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.HNSW
{
    public class HistogramValue
    {
        public int Index { get; internal set; }
        public int Value { get; internal set; }
        public float MinBound { get; internal set; }
        public float MaxBound { get; internal set; }
    }

    public class Histogram
    {
        public HistogramMode Mode { get; }
        public float Min { get; }
        public float Max { get; }
        public float BoundsPeriod { get; }
        public float[] Bounds { get; }
        public int[] Values { get; }

        public Histogram(HistogramMode mode, IList<float> data)
        {
            Mode = mode;
            Min = data.Min();
            Max = data.Max();
            int M = mode == HistogramMode.LOG ? (int)(1f + 3.2f * Math.Log(data.Count)) : (int)(Math.Sqrt(data.Count));
            BoundsPeriod = (Max - Min) / M;
            Bounds = new float[M - 1];

            float bound = Min + BoundsPeriod;
            for (int i = 0; i < Bounds.Length; i++)
            {
                Bounds[i] = bound;
                bound += BoundsPeriod;
            }
            Values = new int[M];
            for (int i = 0; i < Values.Length; i++)
            {
                Values[i] = 0;
            }
            foreach (var v in data)
            {
                if (v < float.Epsilon) continue;
                for (int i = 0; i < Bounds.Length; i++)
                {
                    if (v < Bounds[i])
                    {
                        Values[i]++;
                        break;
                    }
                }
            }
        }

        public int Count => Values?.Length ?? 0;

        public int CountSignChanges()
        {
            if ((Values?.Length ?? 0) <= 2) return 0;
            int i = 0;
            while (Values[i] <= float.Epsilon) { i++; continue; }
            if ((Values.Length - i) <= 2) return 0;

            var delta = Values[i + 1] - Values[i];
            int changes = 0;
            i++;
            for (; i < Values.Length - 1; i++)
            {
                var d = Values[i + 1] - Values[i];
                if (Math.Abs(d) <= float.Epsilon)
                {
                    continue;
                }
                if (NumbersHasSameSign(d, delta) == false)
                {
                    delta = d;
                    changes++;
                }
            }
            return changes;
        }

        public void Smooth()
        {
            var buffer = new int[Values.Length];
            Array.Copy(Values, buffer, buffer.Length);
            for (int i = 2; i < Values.Length - 3; i++)
            {
                Values[i] = (buffer[i - 2] + buffer[i - 1] + buffer[i] + buffer[i + 1] + buffer[i + 2]) / 5;
            }
        }

        public IEnumerable<HistogramValue> GetMaximums()
        {
            var list = new List<HistogramValue>();

            if ((Values?.Length ?? 0) <= 2) return list;
            int i = 0;
            while (Values[i] <= float.Epsilon) { i++; continue; }
            if ((Values.Length - i) <= 2) return list;

            var delta = Values[i + 1] - Values[i];
            i++;
            for (; i < Values.Length - 1; i++)
            {
                var d = Values[i + 1] - Values[i];
                if (Math.Abs(d) <= float.Epsilon)
                {
                    continue;
                }
                if (NumbersHasSameSign(d, delta) == false)
                {
                    if (delta > 0)
                    {
                        list.Add(new HistogramValue
                        {
                            Index = i,
                            Value = Values[i],
                            MinBound = Bounds[i - 1],
                            MaxBound = Bounds[i]
                        });
                    }
                    delta = d;
                }
            }
            return list;
        }

        #region OTSU "https://en.wikipedia.org/wiki/Otsu's_method"
        // function is used to compute the q values in the equation
        private float Px(int init, int end)
        {
            int sum = 0;
            int i;
            for (i = init; i < end; i++)
                sum += Values[i];
            return (float)sum;
        }
        // function is used to compute the mean values in the equation (mu)
        private float Mx(int init, int end)
        {
            int sum = 0;
            int i;
            for (i = init; i < end; i++)
                sum += i * Values[i];

            return (float)sum;
        }

        public int OTSU()
        {
            float p1, p2, p12;
            int k;
            int threshold = 0;
            float bcv = 0;
            for (k = 0; k < Values.Length; k++)
            {
                p1 = Px(0, k);
                p2 = Px(k + 1, Values.Length);
                p12 = p1 * p2;
                if (p12 == 0)
                    p12 = 1;
                float diff = (Mx(0, k) * p2) - (Mx(k + 1, Values.Length) * p1);
                var test = (float)diff * diff / p12;
                if (test > bcv)
                {
                    bcv = test;
                    threshold = k;
                }
            }
            /*
            var local_max = Values[threshold];
            for (int i = threshold + 1; i < Values.Length; i++)
            {
                
            }
            */
            return threshold;
        }
        #endregion

        static bool NumbersHasSameSign(int left, int right)
        {
            return left >= 0 && right >= 0 || left < 0 && right < 0;
        }
    }
}
