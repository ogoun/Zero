namespace ZeroLevel.NN.Architectures
{
    internal class Yolo5
        : SSDNN
    {
        public Yolo5(string modelPath, int width, int height)
            : base(modelPath)
        {
            this.InputH = height;
            this.InputW = width;
        }

        public int InputW { private set; get; }

        public int InputH { private set; get; }
    }
}
