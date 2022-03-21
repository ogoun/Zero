using System.Numerics;
using Zero.NN.Models;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.NN.Models
{
    public class Face
        : IBinarySerializable
    {
        public float X1;
        public float Y1;
        public float X2;
        public float Y2;
        public float Score;
        public Landmarks Landmarks = new Landmarks();

        public float Area => Math.Abs(X2 - X1) * Math.Abs(Y2 - Y1);

        public static float CalculateIoU(Face obj0, Face obj1)
        {
            var interx0 = Math.Max(obj0.X1, obj1.X1);
            var intery0 = Math.Max(obj0.Y1, obj1.Y1);
            var interx1 = Math.Min(obj0.X2, obj1.X2);
            var intery1 = Math.Min(obj0.Y2, obj1.Y2);
            if (interx1 < interx0 || intery1 < intery0) return 0;
            var area0 = obj0.Area;
            var area1 = obj1.Area;
            var areaInter = (interx1 - interx0) * (intery1 - intery0);
            var areaSum = area0 + area1 - areaInter;
            return (float)(areaInter) / areaSum;
        }
        public static void FixInScreen(Face bbox, int width, int height)
        {
            bbox.X1 = Math.Max(0, bbox.X1);
            bbox.Y1 = Math.Max(0, bbox.Y1);
            bbox.X2 = Math.Min(width, bbox.X2);
            bbox.Y2 = Math.Min(height, bbox.Y2);
        }

        public static List<Face> Nms(List<Face> bbox_original_list, float threshold_nms_iou, bool check_class_id)
        {
            var bbox_nms_list = new List<Face>();
            var bbox_list = bbox_original_list.OrderBy(b => b.Score).ToList();
            bool[] is_merged = new bool[bbox_list.Count];
            for (var i = 0; i < bbox_list.Count; i++)
            {
                is_merged[i] = false;
            }
            for (var index_high_score = 0; index_high_score < bbox_list.Count; index_high_score++)
            {
                var candidates = new List<Face>();
                if (is_merged[index_high_score]) continue;

                candidates.Add(bbox_list[index_high_score]);
                for (var index_low_score = index_high_score + 1; index_low_score < bbox_list.Count; index_low_score++)
                {
                    if (is_merged[index_low_score]) continue;
                    if (CalculateIoU(bbox_list[index_high_score], bbox_list[index_low_score]) > threshold_nms_iou)
                    {
                        candidates.Add(bbox_list[index_low_score]);
                        is_merged[index_low_score] = true;
                    }
                }
                bbox_nms_list.Add(candidates[0]);
            }
            return bbox_nms_list;
        }

        // Normalizes a facial image to a standard size given by outSize.
        // Normalization is done based on Dlib's landmark points passed as pointsIn
        // After normalization, left corner of the left eye is at (0.3 * w, h/3 )
        // and right corner of the right eye is at ( 0.7 * w, h / 3) where w and h
        // are the width and height of outSize.
        public static Matrix3x2 GetTransformMatrix(Face face)
        {
            var w = face.X2 - face.X1;
            var h = face.Y2 - face.Y1;

            var leftEyeSrc = new FacePoint((face.Landmarks.LeftEye.X - face.X1) / w, (face.Landmarks.LeftEye.Y - face.Y1) / h);
            var rightEyeSrc = new FacePoint((face.Landmarks.RightEye.X - face.X1) / w, (face.Landmarks.RightEye.Y - face.Y1) / h);

            // Corners of the eye in normalized image
            var leftEyeDst = new FacePoint(0.3f, 1.0f / 3.0f);
            var rightEyeDst = new FacePoint(0.7f, 1.0f / 3.0f);

            return GetTransformMatrix(leftEyeSrc, rightEyeSrc, leftEyeDst, rightEyeDst);
        }

        static Matrix3x2 GetTransformMatrix(FacePoint srcLeftEye, FacePoint srcRightEye,
            FacePoint dstLeftEye, FacePoint dstRightEye)
        {
            var s60 = Math.Sin(60.0f * Math.PI / 180.0f);
            var c60 = Math.Cos(60.0f * Math.PI / 180.0f);

            // The third point is calculated so that the three points make an equilateral triangle
            var xin = c60 * (srcLeftEye.X - srcRightEye.X) - s60 * (srcLeftEye.Y - srcRightEye.Y) + srcRightEye.X;
            var yin = s60 * (srcLeftEye.X - srcRightEye.X) + c60 * (srcLeftEye.Y - srcRightEye.Y) + srcRightEye.Y;

            var xout = c60 * (dstLeftEye.X - dstRightEye.X) - s60 * (dstLeftEye.Y - dstRightEye.Y) + dstRightEye.X;
            var yout = s60 * (dstLeftEye.X - dstRightEye.X) + c60 * (dstLeftEye.Y - dstRightEye.Y) + dstRightEye.Y;

            System.Drawing.PointF[] source = {
                new System.Drawing.PointF(srcLeftEye.X, srcLeftEye.Y),
                new System.Drawing.PointF(srcRightEye.X, srcRightEye.Y),
                new System.Drawing.PointF((float)xin, (float)yin)
            };
            System.Drawing.PointF[] target = {
                new System.Drawing.PointF(dstLeftEye.X, dstLeftEye.Y),
                new System.Drawing.PointF(dstRightEye.X, dstRightEye.Y),
                new System.Drawing.PointF((float)xout, (float)yout)
            };
            Aurigma.GraphicsMill.Transforms.Matrix matrix =
                Aurigma.GraphicsMill.Transforms.Matrix.CreateFromAffinePoints(source, target);

            return new Matrix3x2(
                matrix.Elements[0], matrix.Elements[1],
                matrix.Elements[3], matrix.Elements[4],
                matrix.Elements[6], matrix.Elements[7]);
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteFloat(this.X1);
            writer.WriteFloat(this.Y1);
            writer.WriteFloat(this.X2);
            writer.WriteFloat(this.Y2);
            writer.WriteFloat(this.Score);
            writer.Write(this.Landmarks);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.X1 = reader.ReadFloat();
            this.Y1 = reader.ReadFloat();
            this.X2 = reader.ReadFloat();
            this.Y2 = reader.ReadFloat();
            this.Score = reader.ReadFloat();
            this.Landmarks = reader.Read<Landmarks>();
        }
    }
}
