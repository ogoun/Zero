namespace ZeroLevel.NN.Models
{
    /// <summary>
    /// Crop options
    /// </summary>
    public class ImagePreprocessorCropOptions
    {
        /// <summary>
        /// Use split original image to crops
        /// </summary>
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Put resized original image to batch
        /// </summary>
        public bool SaveOriginal { get; set; }
        /// <summary>
        /// Crop width
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Crop height
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// Overlap cropped parts
        /// </summary>
        public bool Overlap { get; set; }
        /// <summary>
        /// Overlap width koefficient (0 - 1)
        /// </summary>
        public float OverlapKoefWidth { get; set; } = 0.8f;
        /// <summary>
        /// Overlap height koefficient (0 - 1)
        /// </summary>
        public float OverlapKoefHeight { get; set; } = 0.8f;
    }
}
