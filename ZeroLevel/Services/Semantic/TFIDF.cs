using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Services.Semantic.Helpers;

namespace ZeroLevel.Services.Semantic
{
    public class IDF
    {
        private ConcurrentDictionary<string, int> _terms =
            new ConcurrentDictionary<string, int>();
        private long _documents_count = 0;

        public void Append(BagOfTerms bag)
        {
            _documents_count++;
            foreach (var term in bag.ToUniqueTokens())
            {
                _terms.AddOrUpdate(term, 1, (w, o) => o + 1);
            }
        }

        public double Idf(string term)
        {
            if (_terms.ContainsKey(term))
            {
                double count_documents_with_term = (double)_terms[term];
                double total_documents = (double)_documents_count;
                return Math.Log(1.0d + (total_documents / count_documents_with_term));
            }
            return 0.0d;
        }
    }

    public static class TFIDF
    {
        private static readonly IReadOnlyDictionary<string, double> _empty = new Dictionary<string, double>();

        public static IReadOnlyDictionary<string, double> TfIdf(BagOfTerms document, IDF idf)
        {
            if (document.Words.Length > 0)
            {
                var freg = document.Freguency();
                return document
                    .ToUniqueTokensWithoutStopWords()
                    .ToDictionary(t => t, t => idf.Idf(t) * (double)freg[t] / (double)document.Words.Length);
            }
            return _empty;
        }

        public static IReadOnlyDictionary<string, double> TfIdf_Smooth(BagOfTerms document, IDF idf)
        {
            if (document.Words.Length > 0)
            {
                var freg = document.Freguency();
                var max = (double)freg.Max(f => f.Value);
                return document
                    .ToUniqueTokensWithoutStopWords()
                    .ToDictionary(t => t, t => idf.Idf(t) * (0.5d + 0.5d * ((double)freg[t] / max)));
            }
            return _empty;
        }

        public static IReadOnlyDictionary<string, double> Tf(BagOfTerms document)
        {
            if (document.Words.Length > 0)
            {
                var freg = document.Freguency();
                return document
                    .ToUniqueTokensWithoutStopWords()
                    .ToDictionary(t => t, t => (double)freg[t] / (double)document.Words.Length);
            }
            return _empty;
        }

        public static IReadOnlyDictionary<string, double> Tf_Smooth(BagOfTerms document)
        {
            if (document.Words.Length > 0)
            {
                var freg = document.Freguency();
                var max = (double)freg.Max(f => f.Value);
                return document
                    .ToUniqueTokensWithoutStopWords()
                    .ToDictionary(t => t, t => (0.5d + 0.5d * ((double)freg[t] / max)));
            }
            return _empty;
        }
    }
}
