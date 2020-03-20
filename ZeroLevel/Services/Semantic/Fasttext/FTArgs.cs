using System;
using System.Text;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Semantic.Fasttext
{
    public class FTArgs
        : IBinarySerializable
    {
        #region Args
        public string input;
        public string output;

        public double lr;
        public int lrUpdateRate;
        public int dim;
        public int ws;
        public int epoch;
        public int minCount;
        public int minCountLabel;
        public int neg;
        public int wordNgrams;
        public loss_name loss;
        public model_name model;
        public int bucket;
        public int minn;
        public int maxn;
        public int thread;
        public double t;
        public string label;
        public int verbose;
        public string pretrainedVectors;
        public bool saveOutput;
        public bool qout;
        public bool retrain;
        public bool qnorm;
        public ulong cutoff;
        public ulong dsub;
        #endregion

        public FTArgs()
        {
            lr = 0.05;
            dim = 100;
            ws = 5;
            epoch = 5;
            minCount = 5;
            minCountLabel = 0;
            neg = 5;
            wordNgrams = 1;
            loss = loss_name.ns;
            model = model_name.sg;
            bucket = 2000000;
            minn = 3;
            maxn = 6;
            thread = 12;
            lrUpdateRate = 100;
            t = 1e-4;
            label = "__label__";
            verbose = 2;
            pretrainedVectors = "";
            saveOutput = false;
            qout = false;
            retrain = false;
            qnorm = false;
            cutoff = 0;
            dsub = 2;
        }

        protected string lossToString(loss_name ln)
        {
            switch (ln)
            {
                case loss_name.hs:
                    return "hs";
                case loss_name.ns:
                    return "ns";
                case loss_name.softmax:
                    return "softmax";
            }
            return "Unknown loss!"; // should never happen
        }

        protected string boolToString(bool b)
        {
            if (b)
            {
                return "true";
            }
            else
            {
                return "false";
            }
        }

        protected string modelToString(model_name mn)
        {
            switch (mn)
            {
                case model_name.cbow:
                    return "cbow";
                case model_name.sg:
                    return "sg";
                case model_name.sup:
                    return "sup";
            }
            return "Unknown model name!"; // should never happen
        }

        #region Help
        public string printHelp()
        {
            return
                printBasicHelp() +
                printDictionaryHelp() +
                printTrainingHelp() +
                printQuantizationHelp();
        }


        private string printBasicHelp()
        {
            return "\nThe following arguments are mandatory:\n" +
              "  -input              training file path\n" +
              "  -output             output file path\n" +
              "\nThe following arguments are optional:\n" +
              "  -verbose            verbosity level [" + verbose + "]\n";
        }

        private string printDictionaryHelp()
        {
            return
              "\nThe following arguments for the dictionary are optional:\n" +
              "  -minCount           minimal number of word occurences [" + minCount + "]\n" +
              "  -minCountLabel      minimal number of label occurences [" + minCountLabel + "]\n" +
              "  -wordNgrams         max length of word ngram [" + wordNgrams + "]\n" +
              "  -bucket             number of buckets [" + bucket + "]\n" +
              "  -minn               min length of char ngram [" + minn + "]\n" +
              "  -maxn               max length of char ngram [" + maxn + "]\n" +
              "  -t                  sampling threshold [" + t + "]\n" +
              "  -label              labels prefix [" + label + "]\n";
        }

        private string printTrainingHelp()
        {
            return
              "\nThe following arguments for training are optional:\n" +
              "  -lr                 learning rate [" + lr + "]\n" +
              "  -lrUpdateRate       change the rate of updates for the learning rate [" + lrUpdateRate + "]\n" +
              "  -dim                size of word vectors [" + dim + "]\n" +
              "  -ws                 size of the context window [" + ws + "]\n" +
              "  -epoch              number of epochs [" + epoch + "]\n" +
              "  -neg                number of negatives sampled [" + neg + "]\n" +
              "  -loss               loss function {ns, hs, softmax} [" + lossToString(loss) + "]\n" +
              "  -thread             number of threads [" + thread + "]\n" +
              "  -pretrainedVectors  pretrained word vectors for supervised learning [" + pretrainedVectors + "]\n" +
              "  -saveOutput         whether output params should be saved [" + boolToString(saveOutput) + "]\n";
        }

        private string printQuantizationHelp()
        {
            return
              "\nThe following arguments for quantization are optional:\n" +
              "  -cutoff             number of words and ngrams to retain [" + cutoff + "]\n" +
              "  -retrain            whether embeddings are finetuned if a cutoff is applied [" + boolToString(retrain) + "]\n" +
              "  -qnorm              whether the norm is quantized separately [" + boolToString(qnorm) + "]\n" +
              "  -qout               whether the classifier is quantized [" + boolToString(qout) + "]\n" +
              "  -dsub               size of each sub-vector [" + dsub + "]\n";
        }
        #endregion

        public void parseArgs(params string[] args)
        {
            var command = args[1];
            if (command.Equals("supervised", System.StringComparison.OrdinalIgnoreCase))
            {
                model = model_name.sup;
                loss = loss_name.softmax;
                minCount = 1;
                minn = 0;
                maxn = 0;
                lr = 0.1;
            }
            else if (command.Equals("cbow", System.StringComparison.OrdinalIgnoreCase))
            {
                model = model_name.cbow;
            }
            for (int ai = 2; ai < args.Length; ai += 2)
            {
                if (args[ai][0] != '-')
                {
                    Log.Warning("Provided argument without a dash! Usage: " + printHelp());
                }
                try
                {
                    if (args[ai].Equals("-h", System.StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Warning("Here is the help! Usage: " + printHelp());
                    }
                    else if (args[ai].Equals("-input", System.StringComparison.OrdinalIgnoreCase))
                    {
                        input = args[ai + 1];
                    }
                    else if (args[ai].Equals("-output", System.StringComparison.OrdinalIgnoreCase))
                    {
                        output = args[ai + 1];
                    }
                    else if (args[ai].Equals("-lr", System.StringComparison.OrdinalIgnoreCase))
                    {
                        lr = double.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-lrUpdateRate", System.StringComparison.OrdinalIgnoreCase))
                    {
                        lrUpdateRate = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-dim", System.StringComparison.OrdinalIgnoreCase))
                    {
                        dim = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-ws", System.StringComparison.OrdinalIgnoreCase))
                    {
                        ws = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-epoch", System.StringComparison.OrdinalIgnoreCase))
                    {
                        epoch = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-minCount", System.StringComparison.OrdinalIgnoreCase))
                    {
                        minCount = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-minCountLabel", System.StringComparison.OrdinalIgnoreCase))
                    {
                        minCountLabel = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-neg", System.StringComparison.OrdinalIgnoreCase))
                    {
                        neg = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-wordNgrams", System.StringComparison.OrdinalIgnoreCase))
                    {
                        wordNgrams = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-loss", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (args[ai + 1].Equals("hs", System.StringComparison.OrdinalIgnoreCase))
                        {
                            loss = loss_name.hs;
                        }
                        else if (args[ai + 1].Equals("ns", System.StringComparison.OrdinalIgnoreCase))
                        {
                            loss = loss_name.ns;
                        }
                        else if (args[ai + 1].Equals("softmax", System.StringComparison.OrdinalIgnoreCase))
                        {
                            loss = loss_name.softmax;
                        }
                        else
                        {
                            loss = loss_name.ns;
                            Log.Warning("Unknown loss! Usage: " + printHelp());
                        }
                    }
                    else if (args[ai].Equals("-bucket", System.StringComparison.OrdinalIgnoreCase))
                    {
                        bucket = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-minn", System.StringComparison.OrdinalIgnoreCase))
                    {
                        minn = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-maxn", System.StringComparison.OrdinalIgnoreCase))
                    {
                        maxn = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-thread", System.StringComparison.OrdinalIgnoreCase))
                    {
                        thread = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-t", System.StringComparison.OrdinalIgnoreCase))
                    {
                        t = double.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-label", System.StringComparison.OrdinalIgnoreCase))
                    {
                        label = args[ai + 1];
                    }
                    else if (args[ai].Equals("-verbose", System.StringComparison.OrdinalIgnoreCase))
                    {
                        verbose = int.Parse(args[ai + 1]);
                    }
                    else if (args[ai].Equals("-pretrainedVectors", System.StringComparison.OrdinalIgnoreCase))
                    {
                        pretrainedVectors = args[ai + 1];
                    }
                    else if (args[ai].Equals("-saveOutput", System.StringComparison.OrdinalIgnoreCase))
                    {
                        saveOutput = true;
                        ai--;
                    }
                    else if (args[ai].Equals("-qnorm", System.StringComparison.OrdinalIgnoreCase))
                    {
                        qnorm = true;
                        ai--;
                    }
                    else if (args[ai].Equals("-retrain", System.StringComparison.OrdinalIgnoreCase))
                    {
                        retrain = true;
                        ai--;
                    }
                    else if (args[ai].Equals("-qout", System.StringComparison.OrdinalIgnoreCase))
                    {
                        qout = true;
                        ai--;
                    }
                    else if (args[ai].Equals("-cutoff", System.StringComparison.OrdinalIgnoreCase))
                    {
                        cutoff = ulong.Parse(args[ai + 1]);
                    }
                    else if (args[ai] == "-dsub")
                    {
                        dsub = ulong.Parse(args[ai + 1]);
                    }
                    else
                    {
                        Log.Warning("Unknown argument: " + args[ai] + "! Usage: " + printHelp());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                }
            }
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
            {
                throw new Exception("Empty input or output path.\r\n" + printHelp());
            }
            if (wordNgrams <= 1 && maxn == 0)
            {
                bucket = 0;
            }
        }

        public void parseArgs(IConfiguration config)
        {
            if (config.Contains("supervised"))
            {
                model = model_name.sup;
                loss = loss_name.softmax;
                minCount = 1;
                minn = 0;
                maxn = 0;
                lr = 0.1;
            }
            else if (config.Contains("cbow"))
            {
                model = model_name.cbow;
            }
            foreach (var key in config.Keys)
            {
                switch (key)
                {
                    case "input":
                    case "-input":
                        input = config.First(key);
                        break;
                    case "output":
                    case "-output":
                        output = config.First(key);
                        break;
                    case "lr":
                    case "-lr":
                        lr = config.First<double>(key);
                        break;
                    case "":
                        break;
                    case "lrUpdateRate":
                    case "-lrUpdateRate":
                        lrUpdateRate = config.First<int>(key);
                        break;
                    case "dim":
                    case "-dim":
                        dim = config.First<int>(key);
                        break;
                    case "ws":
                    case "-ws":
                        ws = config.First<int>(key);
                        break;
                    case "epoch":
                    case "-epoch":
                        epoch = config.First<int>(key);
                        break;
                    case "minCount":
                    case "-minCount":
                        minCount = config.First<int>(key);
                        break;
                    case "minCountLabel":
                    case "-minCountLabel":
                        minCountLabel = config.First<int>(key);
                        break;
                    case "neg":
                    case "-neg":
                        neg = config.First<int>(key);
                        break;
                    case "wordNgrams":
                    case "-wordNgrams":
                        wordNgrams = config.First<int>(key);
                        break;
                    case "loss":
                    case "-loss":
                        switch (config.First(key))
                        {
                            case "hs": loss = loss_name.hs; break;
                            case "ns": loss = loss_name.ns; break;
                            case "softmax": loss = loss_name.softmax; break;
                            default: Log.Warning("Unknown loss! Usage: " + printHelp()); break;
                        }
                        break;
                    case "bucket":
                    case "-bucket":
                        bucket = config.First<int>(key);
                        break;
                    case "minn":
                    case "-minn":
                        minn = config.First<int>(key);
                        break;
                    case "maxn":
                    case "-maxn":
                        maxn = config.First<int>(key);
                        break;
                    case "thread":
                    case "-thread":
                        thread = config.First<int>(key);
                        break;
                    case "t":
                    case "-t":
                        t = config.First<double>(key);
                        break;
                    case "label":
                    case "-label":
                        label = config.First(key);
                        break;
                    case "verbose":
                    case "-verbose":
                        verbose = config.First<int>(key);
                        break;
                    case "pretrainedVectors":
                    case "-pretrainedVectors":
                        pretrainedVectors = config.First(key);
                        break;
                    case "saveOutput":
                    case "-saveOutput":
                        saveOutput = true;
                        break;
                    case "qnorm":
                    case "-qnorm":
                        qnorm = true;
                        break;
                    case "retrain":
                    case "-retrain":
                        retrain = true;
                        break;
                    case "qout":
                        qout = true;
                        break;
                    case "cutoff":
                        cutoff = config.First<ulong>(key);
                        break;
                    case "dsub":
                        dsub = config.First<ulong>(key);
                        break;
                }
            }
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
            {
                throw new Exception("Empty input or output path.\r\n" + printHelp());
            }
            if (wordNgrams <= 1 && maxn == 0)
            {
                bucket = 0;
            }
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(dim);
            writer.WriteInt32(ws);
            writer.WriteInt32(epoch);
            writer.WriteInt32(minCount);
            writer.WriteInt32(neg);
            writer.WriteInt32(wordNgrams);
            writer.WriteInt32((int)loss);
            writer.WriteInt32((int)model);
            writer.WriteInt32(bucket);
            writer.WriteInt32(minn);
            writer.WriteInt32(maxn);
            writer.WriteInt32(lrUpdateRate);
            writer.WriteDouble(t);
        }

        public void Deserialize(IBinaryReader reader)
        {
            dim = reader.ReadInt32();
            ws = reader.ReadInt32();
            epoch = reader.ReadInt32();
            minCount = reader.ReadInt32();
            neg = reader.ReadInt32();
            wordNgrams = reader.ReadInt32();
            loss = (loss_name)reader.ReadInt32();
            model = (model_name)reader.ReadInt32();
            bucket = reader.ReadInt32();
            minn = reader.ReadInt32();
            maxn = reader.ReadInt32();
            lrUpdateRate = reader.ReadInt32();
            t = reader.ReadDouble();
        }

        public string dump()
        {
            var dump = new StringBuilder();
            dump.AppendLine($"dim {dim}");
            dump.AppendLine($"ws {ws}");
            dump.AppendLine($"epoch {epoch}");
            dump.AppendLine($"minCount {minCount}");
            dump.AppendLine($"neg {neg}");
            dump.AppendLine($"wordNgrams {wordNgrams}");
            dump.AppendLine($"loss {lossToString(loss)}");
            dump.AppendLine($"model {modelToString(model)}");
            dump.AppendLine($"bucket {bucket}");
            dump.AppendLine($"minn {minn}");
            dump.AppendLine($"maxn {maxn}");
            dump.AppendLine($"lrUpdateRate {lrUpdateRate}");
            dump.AppendLine($"t {t}");
            return dump.ToString();
        }
    }
}
