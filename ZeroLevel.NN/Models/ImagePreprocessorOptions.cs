namespace ZeroLevel.NN.Models
{
    public class ImagePreprocessorOptions
    {
        private const float PIXEL_NORMALIZATION_SCALE = 1f / 255f;
        public ImagePreprocessorOptions(int inputWidth, int inputHeight, PredictorChannelType channelType)
        {
            this.InputWidth = inputWidth;
            this.InputHeight = inputHeight;
            this.ChannelType = channelType;
        }

        public ImagePreprocessorOptions UseCrop(int width, int height, bool saveOriginal, bool overlap)
        {
            Crop.Enabled = true;
            Crop.Height = height;
            Crop.Width = width;
            Crop.Overlap = overlap;
            Crop.SaveOriginal = saveOriginal;
            return this;
        }

        public ImagePreprocessorOptions ApplyNormilization(float? multiplier = null)
        {
            if (multiplier.HasValue)
            {
                NormalizationMultiplier = multiplier.Value;
            }
            this.Normalize = true;
            return this;
        }

        public ImagePreprocessorOptions ApplyAxeInversion()
        {
            this.InvertXY = true;
            return this;
        }

        public ImagePreprocessorOptions ApplyCorrection(float[] mean, float[] std)
        {
            if (this.Correction)
            {
                throw new InvalidOperationException("Correction setup already");
            }
            this.Correction = true;
            this.Mean = mean;
            this.Std = std;
            return this;
        }

        public ImagePreprocessorOptions ApplyCorrection(Func<int, float, float> correctionFunc)
        {
            if (this.Correction)
            {
                throw new InvalidOperationException("Correction setup already");
            }
            this.Correction = true;
            this.CorrectionFunc = correctionFunc;
            return this;
        }
        
        public ImagePreprocessorOptions UseBGR()
        {
            this.BGR = true;
            return this;
        }


        public float NormalizationMultiplier { get; private set; } = PIXEL_NORMALIZATION_SCALE;
        /// <summary>
        /// Channel type, if first tensor dims = [batch_index, channel, x, y], if last, dims = dims = [batch_index, x, y, channel]
        /// </summary>
        public PredictorChannelType ChannelType { get; private set; }
        /// <summary>
        /// Ctop image options
        /// </summary>
        public ImagePreprocessorCropOptions Crop { get; } = new ImagePreprocessorCropOptions();
        /// <summary>
        /// NN model input height
        /// </summary>
        public int InputHeight { get; private set; }
        /// <summary>
        /// NN model input width
        /// </summary>
        public int InputWidth { get; private set; }
        /// <summary>
        /// Transfrom pixel values to (0-1) range
        /// </summary>
        public bool Normalize { get; private set; } = false;
        /// <summary>
        /// Transform pixel value with mean/std values v=(v-mean)/std
        /// </summary>
        public bool Correction { get; private set; } = false;
        /// <summary>
        /// Mean values if Correction parameter is true
        /// </summary>

        public Func<int, float, float> CorrectionFunc { get; private set; } = null;

        public float[] Mean { get; private set; }
        /// <summary>
        /// Std values if Correction parameter is true
        /// </summary>
        public float[] Std { get; private set; }
        /// <summary>
        /// Put pixel values to tensor in BGR order
        /// </summary>
        public bool BGR { get; set; } = false;
        /// <summary>
        /// Invert width and height in input tensor
        /// </summary>
        public bool InvertXY { get; set; } = false;
        /// <summary>
        /// Channel count (auto calculate)
        /// </summary>
        public int Channels { get; set; }
        /// <summary>
        /// Maximum batch size, decrease if video memory overflow
        /// </summary>
        public int MaxBatchSize { get; set; } = 13;
    }
}
