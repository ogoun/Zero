using System;
using System.Collections.Generic;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN
{
    public static class NMS
    {
        private const float IOU_THRESHOLD = .01f;

        public static void Apply(List<YoloPrediction> boxes)
        {
            for (int i = 0; i < boxes.Count - 1; i++)
            {
                var left = boxes[i];
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    var right = boxes[j];
                    if (left.Class == right.Class)
                    {
                        var iou = IOU(left, right);
                        if (iou > IOU_THRESHOLD)
                        {
                            if (left.Score > right.Score)
                            {
                                boxes.RemoveAt(j);
                                j--;
                            }
                            else
                            {
                                boxes.RemoveAt(i);
                                i--;
                                break;
                            }
                        }
                    }
                }
            }
        }

        static float IOU(YoloPrediction box1, YoloPrediction box2)
        {
            var left = (float)Math.Max(box1[0], box2[0]);
            var right = (float)Math.Min(box1[2], box2[2]);

            var top = (float)Math.Max(box1[1], box2[1]);
            var bottom = (float)Math.Min(box1[3], box2[3]);

            var width = right - left;
            var height = bottom - top;
            var intersectionArea = width * height;

            return intersectionArea / (float)(box1.Area + box2.Area - intersectionArea);
        }
    }
}
