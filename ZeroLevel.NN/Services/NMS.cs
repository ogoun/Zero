using ZeroLevel.NN.Models;

namespace ZeroLevel.NN.Services
{
    public static class NMS
    {
        private const float IOU_THRESHOLD = .2f;
        private const float IOU_MERGE_THRESHOLD = .5f;

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
                        // удаление вложенных боксов
                        var ni = NestingIndex(left, right);
                        if (ni == 1)
                        {
                            boxes.RemoveAt(i);
                            i--;
                            break;
                        }
                        else if (ni == 2)
                        {
                            boxes.RemoveAt(j);
                            j--;
                        }
                        // -----------------------------
                        else
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
                            /*else if (threshold > 0.01f && iou > 0.2f)
                            {
                                // UNION
                                var x1 = Math.Min(left.X, right.X);
                                var y1 = Math.Min(left.Y, right.Y);
                                var x2 = Math.Max(left.X + left.W, right.X + right.W);
                                var y2 = Math.Max(left.Y + left.H, right.Y + right.H);
                                var w = x2 - x1;
                                var h = y2 - y1;
                                boxes.RemoveAt(j);
                                boxes.RemoveAt(i);
                                boxes.Add(new Prediction
                                {
                                    Class = left.Class,
                                    Label = left.Label,
                                    Score = Math.Max(left.Score, right.Score),
                                    Cx = x1 + w / 2.0f,
                                    Cy = y1 + h / 2.0f,
                                    W = w,
                                    H = h
                                });
                                i--;
                                break;
                            }*/
                        }
                    }
                }
            }
        }

        /// <summary>
        /// проверка на вложенность боксов
        /// </summary>
        static int NestingIndex(YoloPrediction box1, YoloPrediction box2)
        {
            if (box1.X > box2.X &&
                box1.Y > box2.Y &&
                (box1.X + box1.W) < (box2.X + box2.W) &&
                (box1.Y + box1.H) < (box2.Y + box2.H))
            {
                return 1;
            }
            if (box2.X > box1.X &&
                box2.Y > box1.Y &&
                (box2.X + box2.W) < (box1.X + box1.W) &&
                (box2.Y + box2.H) < (box1.Y + box1.H))
            {
                return 2;
            }
            return 0;
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
