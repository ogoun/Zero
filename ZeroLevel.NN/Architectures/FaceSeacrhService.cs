using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ZeroLevel;
using ZeroLevel.NN;
using ZeroLevel.NN.Models;

namespace Zero.NN.Services
{
    public class FaceSeacrhService
    {
        private readonly IFaceDetector _detector;
        private readonly IEncoder _encoder;
        private readonly bool _useFaceAlign;
        public FaceSeacrhService(IFaceDetector detector, IEncoder encoder, bool useFaceAlign = true)
        {
            _useFaceAlign = useFaceAlign;
            _detector = detector;
            _encoder = encoder;
        }

        public static Image MakeEyesHorizontal(Image source, Face face)
        {
            // положение глаз для определения угла поворота
            var leftEye = face.Landmarks.LeftEye;
            var rightEye = face.Landmarks.RightEye;
            var dY = rightEye.Y - leftEye.Y;
            var dX = rightEye.X - leftEye.X;
            // угол на который нужно повернуть изображение чтбы выравнять глаза
            var ra = (float)Math.Atan2(dY, dX);

            // определить размеры и центр лица
            var minX = face.Landmarks.Left();
            var minY = face.Landmarks.Top();
            var maxX = face.Landmarks.Right();
            var maxY = face.Landmarks.Bottom();

            var centerFaceX = (maxX + minX) / 2.0f;
            var centerFaceY = (maxY + minY) / 2.0f;

            // определить описывающий лицо прямоугольник с центром в centerFaceX;centerFaceY
            var distanceX = face.X2 - face.X1;
            var distanceY = face.Y2 - face.Y1;

            var dx = (face.X1 + distanceX / 2.0f) - centerFaceX;
            var dy = (face.Y1 + distanceY / 2.0f) - centerFaceY;

            var x1 = face.X1 - dx;
            var y1 = face.Y1 - dy;
            var x2 = face.X2 - dx;
            var y2 = face.Y2 - dy;

            // определить квадрат описывающий прямоугольник с лицом повернутый на 45 градусов
            var radius = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1)) / 2.0f;
            x1 = centerFaceX - radius;
            x2 = centerFaceX + radius;
            y1 = centerFaceY - radius;
            y2 = centerFaceY + radius;

            var cropDx = radius - distanceX / 2.0f;
            var cropDy = radius - distanceY / 2.0f;

            using (var fullCrop = ImagePreprocessor.Crop(source, x1, y1, x2, y2))
            {
                fullCrop.Mutate(img => img.Rotate((float)(-ra * (180.0f / Math.PI)), KnownResamplers.Bicubic));
                var crop = ImagePreprocessor.Crop(fullCrop, cropDx, cropDy, fullCrop.Width - cropDx, fullCrop.Height - cropDy);
                crop.Mutate(img => img.Resize(112, 112, KnownResamplers.Bicubic));
                return crop;
            }
        }
        private Image SpecialCrop(Image image, Face face)
        {
            var left = face.Landmarks.Left();       //  0.3
            var right = face.Landmarks.Right();     //  0.7
            var top = face.Landmarks.Top();         //  0.4
            var bottom = face.Landmarks.Bottom();   //  0.8

            var newWidth = (right - left) / 0.4f;
            var newHeight = (bottom - top) / 0.4f;

            // привести к квадрату !

            var cx1 = left - (newWidth * 0.3f);
            var cy1 = top - (newHeight * 0.4f);
            var cx2 = cx1 + newWidth;
            var cy2 = cy1 + newHeight;

            var clipX = new Func<float, float>(x =>
            {
                if (x < 0) return 0;
                if (x > image.Width) return image.Width;
                return x;
            });
            var clipY = new Func<float, float>(y =>
            {
                if (y < 0) return 0;
                if (y > image.Height) return image.Height;
                return y;
            });

            cx1 = clipX(cx1);
            cx2 = clipX(cx2);
            cy1 = clipY(cy1);
            cy2 = clipY(cy2);

            return ImagePreprocessor.Crop(image, cx1, cy1, cx2, cy2);
        }

        public IEnumerable<FaceEmbedding> GetEmbeddings(Image<Rgb24> image)
        {
            int width = image.Width;
            int heigth = image.Height;
            var faces = _detector.Predict(image);
            foreach (var face in faces)
            {
                Face.FixInScreen(face, width, heigth);
                float[] vector;
                if (_useFaceAlign)
                {
                    int x = (int)face.X1;
                    int y = (int)face.Y1;
                    int w = (int)(face.X2 - face.X1);
                    int h = (int)(face.Y2 - face.Y1);
                    var radius = (float)Math.Sqrt(w * w + h * h) / 2f;
                    var centerFaceX = (face.X2 + face.X1) / 2.0f;
                    var centerFaceY = (face.Y2 + face.Y1) / 2.0f;
                    var around_x1 = centerFaceX - radius;
                    var around_x2 = centerFaceX + radius;
                    var around_y1 = centerFaceY - radius;
                    var around_y2 = centerFaceY + radius;
                    try
                    {
                        using (var faceImage = ImagePreprocessor.Crop(image, around_x1, around_y1, around_x2, around_y2))
                        {
                            var matrix = Face.GetTransformMatrix(face);
                            var builder = new AffineTransformBuilder();
                            builder.AppendMatrix(matrix);
                            faceImage.Mutate(x => x.Transform(builder, KnownResamplers.Bicubic));
                            vector = _encoder.Predict(faceImage);
                            /*var aligned_faces = detector.Predict(faceImage);
                            if (aligned_faces != null && aligned_faces.Count == 1)
                            {
                                using (var ci = SpecialCrop(faceImage, aligned_faces[0]))
                                {
                                    vector = encoder.Predict(faceImage);
                                }
                            }
                            else
                            {
                                vector = encoder.Predict(faceImage);
                            }*/
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, "[FaceSeacrhService.GetEmbeddings]");
                        using (var faceImage = ImagePreprocessor.Crop(image, face.X1, face.Y1, face.X2, face.Y2))
                        {
                            vector = _encoder.Predict(faceImage);
                        }
                    }
                }
                else
                {
                    using (var faceImage = ImagePreprocessor.Crop(image, face.X1, face.Y1, face.X2, face.Y2))
                    {
                        vector = _encoder.Predict(faceImage);
                    }
                }
                yield return new FaceEmbedding
                {
                    Face = face,
                    Vector = vector
                };
            }
        }


        public IEnumerable<(FaceEmbedding, Image)> GetEmbeddingsAndCrop(Image<Rgb24> image)
        {
            int width = image.Width;
            int heigth = image.Height;
            var faces = _detector.Predict(image);
            foreach (var face in faces)
            {
                Face.FixInScreen(face, width, heigth);
                float[] vector;
                if (_useFaceAlign)
                {
                    int x = (int)face.X1;
                    int y = (int)face.Y1;
                    int w = (int)(face.X2 - face.X1);
                    int h = (int)(face.Y2 - face.Y1);
                    var radius = (float)Math.Sqrt(w * w + h * h) / 2f;
                    var centerFaceX = (face.X2 + face.X1) / 2.0f;
                    var centerFaceY = (face.Y2 + face.Y1) / 2.0f;
                    var around_x1 = centerFaceX - radius;
                    var around_x2 = centerFaceX + radius;
                    var around_y1 = centerFaceY - radius;
                    var around_y2 = centerFaceY + radius;
                    var faceImage = ImagePreprocessor.Crop(image, around_x1, around_y1, around_x2, around_y2);
                    var matrix = Face.GetTransformMatrix(face);
                    var builder = new AffineTransformBuilder();
                    builder.AppendMatrix(matrix);
                    faceImage.Mutate(x => x.Transform(builder, KnownResamplers.Bicubic));
                    vector = _encoder.Predict(faceImage);
                    yield return (new FaceEmbedding
                    {
                        Face = face,
                        Vector = vector
                    }, faceImage);
                }
                else
                {
                    var faceImage = ImagePreprocessor.Crop(image, face.X1, face.Y1, face.X2, face.Y2);
                    vector = _encoder.Predict(faceImage);
                    yield return (new FaceEmbedding
                    {
                        Face = face,
                        Vector = vector
                    }, faceImage);
                }
            }
        }
    }
}
