using Microsoft.ML.OnnxRuntime.Tensors;
using System.Runtime.CompilerServices;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.ML.DNN.Models
{
    public sealed class TensorPoolItem
        : IBinarySerializable
    {
        public int StartX;
        public int StartY;
        public int Width;
        public int Height;
        public int TensorIndex;
        public Tensor<float> Tensor;

        public TensorPoolItem()
        {
        }
        public TensorPoolItem(Tensor<float> tensor, int tensorIndex, int startX, int startY, int width, int height)
        {
            Tensor = tensor;
            TensorIndex = tensorIndex;
            StartX = startX;
            StartY = startY;
            Width = width;
            Height = height;
        }

        public void Set(int x, int y, float valueR, float valueG, float valueB)
        {
            var tx = x - StartX;
            if (tx < 0 || tx >= Width) return;
            var ty = y - StartY;

            Tensor[TensorIndex, 0, tx, ty] = valueR;
            Tensor[TensorIndex, 1, tx, ty] = valueG;
            Tensor[TensorIndex, 2, tx, ty] = valueB;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastSet(int x, int y, float valueR, float valueG, float valueB)
        {
            Tensor[TensorIndex, 0, x, y] = valueR;
            Tensor[TensorIndex, 1, x, y] = valueG;
            Tensor[TensorIndex, 2, x, y] = valueB;
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(StartX);
            writer.WriteInt32(StartY);
            writer.WriteInt32(Width);
            writer.WriteInt32(Height);
            writer.WriteInt32(TensorIndex);
        }

        public void Deserialize(IBinaryReader reader)
        {
            StartX = reader.ReadInt32();
            StartY = reader.ReadInt32();
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            TensorIndex = reader.ReadInt32();
        }
    }
}
