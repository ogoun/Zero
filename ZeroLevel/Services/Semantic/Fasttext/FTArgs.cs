namespace ZeroLevel.Services.Semantic.Fasttext
{
    public class FTArgs
    {
        #region Args
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
    }
}
