using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using ZeroLevel.NN.Models;

//https://github.com/iwatake2222/play_with_tflite/blob/master/pj_tflite_face_dbface/image_processor/face_detection_engine.cpp

namespace ZeroLevel.NN
{
    public sealed class DBFace
        : SSDNN , IFaceDetector
    {
        private const int STRIDE = 4;
        private const int INPUT_WIDTH = 1216;
        private const int INPUT_HEIGHT = 960;
        private const float THESHOLD = 0.4f;
        private const float IOU_THESHOLD = 0.6f;
        private static float[] MEAN = new[] { 0.408f, 0.447f, 0.47f };
        private static float[] STD = new[] { 0.289f, 0.274f, 0.278f };

        public DBFace(string model_path)
            :base(model_path)
        {
        }

        private static float exp(float v)
        {
            var gate = 1.0f;
            var _base = Math.Exp(gate);
            if (Math.Abs(v) < gate)
                return (float)(v * _base);
            if (v > 0)
            {
                return (float)Math.Exp(v);
            }
            return (float)-Math.Exp(-v);
        }

        private static FacePoint Landmark(float cx, float cy,
            float x, float y,
            float scale_w, float scale_h)
        {
            var p = new FacePoint();
            p.X = (exp(x * 4) + cx) * STRIDE * scale_w;
            p.Y = (exp(y * 4) + cy) * STRIDE * scale_h;
            return p;
        }

        private List<Face> Parse(Tensor<float> hm,
            Tensor<float> boxes, Tensor<float> landmarks,
            int width, int height)
        {
            float x, y, r, b;
            float scale_w = width / (float)(INPUT_WIDTH);
            float scale_h = height / (float)(INPUT_HEIGHT);
            List<Face> bbox_list = new List<Face>();
            for (int cx = 0; cx < hm.Dimensions[3]; cx++)
            {
                for (int cy = 0; cy < hm.Dimensions[2]; cy++)
                {
                    float score = hm[0, 0, cy, cx];
                    if (score >= THESHOLD)
                    {
                        x = boxes[0, 0, cy, cx];
                        y = boxes[0, 1, cy, cx];
                        r = boxes[0, 2, cy, cx];
                        b = boxes[0, 3, cy, cx];

                        x = (cx - x) * STRIDE;
                        y = (cy - y) * STRIDE;
                        r = (cx + r) * STRIDE;
                        b = (cy + b) * STRIDE;

                        var bbox = new Face();
                        bbox.X1 = (int)(x * scale_w);
                        bbox.Y1 = (int)(y * scale_h);
                        bbox.X2 = (int)(r * scale_w);
                        bbox.Y2 = (int)(b * scale_h);
                        bbox.Score = score;

                        bbox.Landmarks.LeftEye = Landmark(cx, cy, landmarks[0, 0, cy, cx], landmarks[0, 5, cy, cx], scale_w, scale_h);
                        bbox.Landmarks.RightEye = Landmark(cx, cy, landmarks[0, 1, cy, cx], landmarks[0, 6, cy, cx], scale_w, scale_h);
                        bbox.Landmarks.Nose = Landmark(cx, cy, landmarks[0, 2, cy, cx], landmarks[0, 7, cy, cx], scale_w, scale_h);
                        bbox.Landmarks.LeftMouth = Landmark(cx, cy, landmarks[0, 3, cy, cx], landmarks[0, 8, cy, cx], scale_w, scale_h);
                        bbox.Landmarks.RightMouth = Landmark(cx, cy, landmarks[0, 4, cy, cx], landmarks[0, 9, cy, cx], scale_w, scale_h);

                        bbox_list.Add(bbox);
                    }
                }
            }
            return bbox_list;
        }

        public IList<Face> Predict(Image image)
        {
            var input = MakeInput(image, 
                new ImagePreprocessorOptions(INPUT_WIDTH, INPUT_HEIGHT, PredictorChannelType.ChannelFirst)
                .ApplyNormilization()
                .ApplyCorrection(MEAN, STD)
                .ApplyAxeInversion());
            List<Face> result = null;
            Extract(new Dictionary<string, Tensor<float>> { { "input", input } }, output =>
            {
                var hm = output["hm"];
                var boxes = output["boxes"];
                var landmark = output["landmarks"];
                result = Parse(hm, boxes, landmark, image.Width, image.Height);
            });
            var cleaned_result = Face.Nms(result, IOU_THESHOLD, false);
            return cleaned_result;
        }
    }
}
