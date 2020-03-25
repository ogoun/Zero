using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel.DataStructures;
using ZeroLevel.Implementation.Semantic.Helpers;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Semantic.Helpers
{
    public class BagOfTerms
    {
        private string[] _words;
        private ILexProvider _lexer;

        public BagOfTerms(string text) : this(TextAnalizer.ExtractWords(text).ToArray(), null) { }

        public BagOfTerms(string text, ILexProvider lexer) : this(TextAnalizer.ExtractWords(text).ToArray(), lexer) { }

        public BagOfTerms(IEnumerable<string> words) : this(words.ToArray(), null) { }

        public BagOfTerms(IEnumerable<string> words, ILexProvider lexer) : this(words.ToArray(), lexer) { }

        public BagOfTerms(string[] words) : this(words, null) { }

        public BagOfTerms(string[] words, ILexProvider lexer)
        {
            _lexer = lexer;
            _frequency = null;
            _words = _lexer == null ? words : _lexer.ExtractLexTokens(words).Select(t => t.Token).ToArray();
        }

        public string[] Words => _words;

        private IDictionary<string, int> _frequency;

        public IDictionary<string, int> Freguency()
        {
            if (_frequency == null)
            {
                var frequency = new Dictionary<string, int>();
                for (int i = 0; i < _words.Length; i++)
                {
                    if (frequency.ContainsKey(_words[i]))
                    {
                        frequency[_words[i]]++;
                    }
                    else
                    {
                        frequency[_words[i]] = 1;
                    }
                }
                _frequency = frequency;
            }
            return _frequency;
        }

        public string[] ToTokens()
        {
            return _words;
        }

        public string[] ToUniqueTokens()
        {
            return _words.DistinctBy(s => s)
                .ToArray();
        }

        public string[] ToUniqueTokensWithoutStopWords()
        {
            return _words.Where(w => StopWords.IsStopWord(w) == false)
                .DistinctBy(s => s)
                .ToArray();
        }
    }



    public class BagOfWords1 :
        IBinarySerializable
    {
        private ConcurrentDictionary<string, int[]> _words;
        int _words_count = -1;
        long _number_of_documents = 0;

        public long NumberOfDocuments => _number_of_documents;
        public int NumberOfWords => _words.Count;

        public BagOfWords1() =>
            _words = new ConcurrentDictionary<string, int[]>();

        /// <summary>
        /// Набор документов, слова в документе должны быть лемматизированы/стеммированы, и быть уникальными
        /// </summary>
        /// <param name="documents"></param>
        public void Learn(string[][] documents)
        {
            Parallel.ForEach(documents, doc =>
            {
                Interlocked.Increment(ref _number_of_documents);
                var partition = new Dictionary<string, int[]>();
                foreach (var word in doc)
                {
                    if (!_words.ContainsKey(word))
                    {
                        if (false == _words.TryAdd(word, new int[2] { Interlocked.Increment(ref _words_count), 1 }))
                        {
                            Interlocked.Increment(ref _words[word][1]);
                        }
                    }
                    else
                    {
                        Interlocked.Increment(ref _words[word][1]);
                    }
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc">Документ - слова в котором должны быть лемматизированы/стеммированы, так же как в модели</param>
        /// <returns></returns>
        public SparceVector Transform(string[] doc)
        {
            if (doc == null || doc.Length == 0) return new SparceVector();
            var map = new Dictionary<string, int>();
            foreach (var word in doc)
            {
                if (map.ContainsKey(word))
                {
                    map[word]++;
                }
                else
                {
                    map[word] = 1;
                }
            }
            var result = new Dictionary<int, double>();
            foreach (var word in doc)
            {
                if (_words.ContainsKey(word) && !result.ContainsKey(_words[word][0]))
                {
                    var tf = (double)map[word] / (double)doc.Length;
                    var idf = Math.Log(1 + (_number_of_documents / _words[word][1]));
                    var tfidf = tf * idf;
                    if (Math.Abs(tfidf) > double.Epsilon)
                    {
                        result.Add(_words[word][0], tfidf);
                    }
                }
            }
            return new SparceVector(result.Values.ToArray(), result.Keys.ToArray());
        }

        public void Deserialize(IBinaryReader reader)
        {
            this._number_of_documents = reader.ReadLong();
            this._words_count = reader.ReadInt32();
            this._words = reader.ReadDictionaryAsConcurrent<string, int[]>();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteLong(this._number_of_documents);
            writer.WriteInt32(this._words_count);
            writer.WriteDictionary<string, int[]>(this._words);
        }
    }
}
