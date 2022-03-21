using SixLabors.ImageSharp;
using ZeroLevel.NN.Models;

namespace ZeroLevel.NN
{
    public interface IFaceDetector
    {
        IList<Face> Predict(Image image);
    }
}
