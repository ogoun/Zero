using ZeroLevel.NN.Models;
using ZeroLevel.Services.Serialization;

namespace Zero.NN.Models
{
    public class Landmarks
        : IBinarySerializable
    {
        public FacePoint RightEye;
        public FacePoint LeftEye;
        public FacePoint Nose;
        public FacePoint RightMouth;
        public FacePoint LeftMouth;

        public float Top()
        {
            var min = RightEye.Y;
            if (LeftEye.Y < min)
            {
                min = LeftEye.Y;
            }
            if (Nose.Y < min)
            {
                min = Nose.Y;
            }
            if (RightMouth.Y < min)
            {
                min = RightMouth.Y;
            }
            if (LeftMouth.Y < min)
            {
                min = LeftMouth.Y;
            }
            return min;
        }

        public float Bottom()
        {
            var max = RightEye.Y;
            if (LeftEye.Y > max)
            {
                max = LeftEye.Y;
            }
            if (Nose.Y > max)
            {
                max = Nose.Y;
            }
            if (RightMouth.Y > max)
            {
                max = RightMouth.Y;
            }
            if (LeftMouth.Y > max)
            {
                max = LeftMouth.Y;
            }
            return max;
        }

        public float Left()
        {
            var min = RightEye.X;
            if (LeftEye.X < min)
            {
                min = LeftEye.X;
            }
            if (Nose.X < min)
            {
                min = Nose.X;
            }
            if (RightMouth.X < min)
            {
                min = RightMouth.X;
            }
            if (LeftMouth.X < min)
            {
                min = LeftMouth.X;
            }
            return min;
        }

        public float Right()
        {
            var max = RightEye.X;
            if (LeftEye.X > max)
            {
                max = LeftEye.X;
            }
            if (Nose.X > max)
            {
                max = Nose.X;
            }
            if (RightMouth.X > max)
            {
                max = RightMouth.X;
            }
            if (LeftMouth.X > max)
            {
                max = LeftMouth.X;
            }
            return max;
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.RightEye = reader.Read<FacePoint>();
            this.LeftEye = reader.Read<FacePoint>();
            this.Nose = reader.Read<FacePoint>();
            this.RightMouth = reader.Read<FacePoint>();
            this.LeftMouth = reader.Read<FacePoint>();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.Write(this.RightEye);
            writer.Write(this.LeftEye);
            writer.Write(this.Nose);
            writer.Write(this.RightMouth);
            writer.Write(this.LeftMouth);
        }
    }
}
