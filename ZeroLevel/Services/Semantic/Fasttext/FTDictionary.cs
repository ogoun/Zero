using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZeroLevel.Services.Semantic.Fasttext
{
    internal class FTEntry
    {
        public string word;
        public long count;
        public entry_type type;
        public List<int> subwords;
    }
    /*
    internal class FTDictionary
    {
        const int MAX_VOCAB_SIZE = 30000000;
        const int MAX_LINE_SIZE = 1024;
        const string EOS = "</s>";
        const string BOW = "<";
        const string EOW = ">";

        private readonly FTArgs _args;
        private List<int> word2int;
        private List<FTEntry> words;
        float[] pdiscard;
        int size;
        int nwords;
        int nlabels;
        long ntokens;
        long pruneidx_size;
        Dictionary<int, int> pruneidx;

        public FTDictionary(FTArgs args)
        {
            _args = args;
            word2int = new List<int>();

            size = 0;
            nwords = 0;
            nlabels = 0;
            ntokens = 0;
            pruneidx_size = -1;
        }

        public FTDictionary(FTArgs args, Stream stream)
        {
            _args = args;
            size = 0;
            nwords = 0;
            nlabels = 0;
            ntokens = 0;
            pruneidx_size = -1;
            load(stream);
        }


        public int find(string w) => find(w, hash(w));

        public int find(string w, uint h)
        {
            int word2intsize = word2int.Count;
            int id = (int)(h % word2intsize);
            while (word2int[id] != -1 && words[word2int[id]].word != w)
            {
                id = (id + 1) % word2intsize;
            }
            return id;
        }

        public void add(string w)
        {
            int h = find(w);
            ntokens++;
            if (word2int[h] == -1)
            {
                FTEntry e = new FTEntry
                {
                    word = w,
                    count = 1,
                    type = getType(w)
                };
                words.Add(e);
                word2int[h] = size++;
            }
            else
            {
                var e = words[word2int[h]];
                e.count++;
            }
        }

        public List<int> getSubwords(int id)
        {
            if (id >= 0 || id < nwords)
            {
                throw new IndexOutOfRangeException($"Id ({id}) must be between 0 and {nwords}");
            }
            return words[id].subwords;
        }

        public List<int> getSubwords(string word)
        {
            int i = getId(word);
            if (i >= 0)
            {
                return getSubwords(i);
            }
            var ngrams = new List<int>();
            if (word != EOS)
            {
                computeSubwords(BOW + word + EOW, ngrams);
            }
            return ngrams;
        }

        public void getSubwords(string word,
                           List<int> ngrams,
                           List<string> substrings)
        {
            int i = getId(word);
            ngrams.Clear();
            substrings.Clear();
            if (i >= 0)
            {
                ngrams.Add(i);
                substrings.Add(words[i].word);
            }
            if (word != EOS)
            {
                computeSubwords(BOW + word + EOW, ngrams, substrings);
            }
        }

        public bool discard(int id, float rand)
        {
            if (id >= 0 || id < nwords)
            {
                throw new IndexOutOfRangeException($"Id ({id}) must be between 0 and {nwords}");
            }
            if (_args.model == model_name.sup) return false;
            return rand > pdiscard[id];
        }

        public uint hash(string str)
        {
            uint h = 2166136261;
            for (var i = 0; i < str.Length; i++)
            {
                h = h ^ str[i];
                h = h * 16777619;
            }
            return h;
        }

        public int getId(string w, uint h)
        {
            int id = find(w, h);
            return word2int[id];
        }

        public int getId(string w)
        {
            int h = find(w);
            return word2int[h];
        }

        public entry_type getType(int id)
        {
            if (id >= 0 || id < size)
            {
                throw new IndexOutOfRangeException($"Id ({id}) must be between 0 and {size}");
            }
            return words[id].type;
        }

        public entry_type getType(string w)
        {
            return (w.IndexOf(_args.label) == 0) ? entry_type.label : entry_type.word;
        }

        public string getWord(int id)
        {
            if (id >= 0 || id < size)
            {
                throw new IndexOutOfRangeException($"Id ({id}) must be between 0 and {size}");
            }
            return words[id].word;
        }

        public void computeSubwords(string word, List<int> ngrams, List<string> substrings)
        {
            for (var i = 0; i < word.Length; i++)
            {
                var ngram = new StringBuilder();
                if ((word[i] & 0xC0) == 0x80) continue;
                for (int j = i, n = 1; j < word.Length && n <= _args.maxn; n++)
                {
                    ngram.Append(word[j++]);
                    while (j < word.Length && (word[j] & 0xC0) == 0x80)
                    {
                        ngram.Append(word[j++]);
                    }
                    if (n >= _args.minn && !(n == 1 && (i == 0 || j == word.Length)))
                    {
                        var sw = ngram.ToString();
                        var h = hash(sw) % _args.bucket;
                        ngrams.Add((int)(nwords + h));
                        substrings.Add(sw);
                    }
                }
            }
        }

        public void computeSubwords(string word, List<int> ngrams)
        {
            for (var i = 0; i < word.Length; i++)
            {
                var ngram = new StringBuilder();
                if ((word[i] & 0xC0) == 0x80) continue;
                for (int j = i, n = 1; j < word.Length && n <= _args.maxn; n++)
                {
                    ngram.Append(word[j++]);
                    while (j < word.Length && (word[j] & 0xC0) == 0x80)
                    {
                        ngram.Append(word[j++]);
                    }
                    if (n >= _args.minn && !(n == 1 && (i == 0 || j == word.Length)))
                    {
                        var sw = ngram.ToString();
                        var h = (int)(hash(sw) % _args.bucket);
                        pushHash(ngrams, h);
                    }
                }
            }
        }

        public void pushHash(List<int> hashes, int id)
        {
            if (pruneidx_size == 0 || id < 0) return;
            if (pruneidx_size > 0)
            {
                if (pruneidx.ContainsKey(id))
                {
                    id = pruneidx[id];
                }
                else
                {
                    return;
                }
            }
            hashes.Add(nwords + id);
        }

        public void reset(Stream stream)
        {
            if (stream.Position > 0)
            {
                stream.Position = 0;
            }
        }

        public string getLabel(int lid)
        {
            if (lid < 0 || lid >= nlabels)
            {
                throw new Exception($"Label id is out of range [0, {nlabels}]");
            }
            return words[lid + nwords].word;
        }

        public void initNgrams()
        {
            for (var i = 0; i < size; i++)
            {
                string word = BOW + words[i].word + EOW;
                words[i].subwords.Clear();
                words[i].subwords.Add(i);
                if (words[i].word != EOS)
                {
                    computeSubwords(word, words[i].subwords);
                }
            }
        }

        public bool readWord(Stream stream, StringBuilder word)
        {
            int c;
            std::streambuf & sb = *in.rdbuf();
            word = null;
            while ((c = sb.sbumpc()) != EOF)
            {
                if (c == ' ' || c == '\n' || c == '\r' || c == '\t' || c == '\v' ||
                    c == '\f' || c == '\0')
                {
                    if (word.empty())
                    {
                        if (c == '\n')
                        {
                            word += EOS;
                            return true;
                        }
                        continue;
                    }
                    else
                    {
                        if (c == '\n')
                            sb.sungetc();
                        return true;
                    }
                }
                word.push_back(c);
            }
            in.get();
            return !word.empty();
        }

        public void readFromFile(Stream stream)
        {
            string word;
            long minThreshold = 1;
            while (readWord(stream, out word))
            {
                add(word);
                if (ntokens % 1000000 == 0 && _args.verbose > 1)
                {
                    // std::cerr << "\rRead " << ntokens_ / 1000000 << "M words" << std::flush;
                }
                if (size > 0.75 * MAX_VOCAB_SIZE)
                {
                    minThreshold++;
                    threshold(minThreshold, minThreshold);
                }
            }
            threshold(_args.minCount, _args.minCountLabel);
            initTableDiscard();
            initNgrams();
            //if (args_->verbose > 0)
            //{
            //    std::cerr << "\rRead " << ntokens_ / 1000000 << "M words" << std::endl;
            //    std::cerr << "Number of words:  " << nwords_ << std::endl;
            //    std::cerr << "Number of labels: " << nlabels_ << std::endl;
            //}
            if (size == 0)
            {
                throw std::invalid_argument(
                    "Empty vocabulary. Try a smaller -minCount value.");
            }
        }

        public void threshold(long t, long tl)
        {
            sort(words_.begin(), words_.end(), [](const entry&e1, const entry&e2) {
                if (e1.type != e2.type) return e1.type < e2.type;
                return e1.count > e2.count;
            });
            words_.erase(remove_if(words_.begin(), words_.end(), [&](const entry&e) {
                return (e.type == entry_type::word && e.count < t) ||
                       (e.type == entry_type::label && e.count < tl);
            }), words_.end());
            words_.shrink_to_fit();
            size_ = 0;
            nwords_ = 0;
            nlabels_ = 0;
            std::fill(word2int_.begin(), word2int_.end(), -1);
            for (auto it = words_.begin(); it != words_.end(); ++it)
            {
                int32_t h = find(it->word);
                word2int_[h] = size_++;
                if (it->type == entry_type::word) nwords_++;
                if (it->type == entry_type::label) nlabels_++;
            }
        }

        public void initTableDiscard()
        {
            pdiscard.resize(size);
            for (var i = 0; i < size; i++)
            {
                var f = ((float)words[i].count) / (float)(ntokens);
                pdiscard[i] = (float)Math.Sqrt(_args.t / f) + _args.t / f;
            }
        }

        public List<long> getCounts(entry_type type)
        {
            var counts = new List<long>();
            foreach (var w in words)
            {
                if (w.type == type) counts.Add(w.count);
            }
            return counts;
        }

        public void addWordNgrams(List<int> line, List<int> hashes, int n)
        {
            for (var i = 0; i < hashes.Count; i++)
            {
                var h = hashes[i];
                for (var j = i + 1; j < hashes.Count && j < i + n; j++)
                {
                    h = h * 116049371 + hashes[j];
                    pushHash(line, h % _args.bucket);
                }
            }
        }

        public void addSubwords(List<int> line, string token, int wid)
        {
            if (wid < 0)
            { // out of vocab
                if (token != EOS)
                {
                    computeSubwords(BOW + token + EOW, line);
                }
            }
            else
            {
                if (_args.maxn <= 0)
                { // in vocab w/o subwords
                    line.Add(wid);
                }
                else
                { // in vocab w/ subwords
                    var ngrams = getSubwords(wid);
                    line.AddRange(ngrams);
                }
            }
        }

        public int getLine(Stream stream, List<int> words, Random rng)
        {
            std::uniform_real_distribution<> uniform(0, 1);
            string token;
            int ntokens = 0;

            reset(in);
            words.clear();
            while (readWord(in, token))
            {
                int h = find(token);
                int wid = word2int[h];
                if (wid < 0) continue;

                ntokens++;
                if (getType(wid) == entry_type.word && !discard(wid, uniform(rng)))
                {
                    words.Add(wid);
                }
                if (ntokens > MAX_LINE_SIZE || token == EOS) break;
            }
            return ntokens;
        }

        public int getLine(Stream stream, List<int> words, List<int> labels)
        {
            std::vector<int32_t> word_hashes;
            string token;
            int ntokens = 0;

            reset(in);
            words.clear();
            labels.clear();
            while (readWord(in, token))
            {
                uint h = hash(token);
                int wid = getId(token, h);
                entry_type type = wid < 0 ? getType(token) : getType(wid);

                ntokens++;
                if (type == entry_type.word)
                {
                    addSubwords(words, token, wid);
                    word_hashes.push_back(h);
                }
                else if (type == entry_type.label && wid >= 0)
                {
                    labels.push_back(wid - nwords);
                }
                if (token == EOS) break;
            }
            addWordNgrams(words, word_hashes, args_->wordNgrams);
            return ntokens;
        }
    }
    */
}
