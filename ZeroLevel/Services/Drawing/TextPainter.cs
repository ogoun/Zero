using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ZeroLevel.Services.Drawing
{
    public static class TextPainter
    {
        #region Drawing text along lines

        public static void DrawOnSegment(Graphics gr, Font font,
            PointF start_point,
            PointF end_point,
            string txt,
            bool text_above_segment)
        {
            int start_ch = 0;
            // gr.DrawLine(Pens.Green, start_point, end_point);
            DrawTextOnSegment(gr, Brushes.Black, font, txt,
                ref start_ch, ref start_point, end_point, text_above_segment);
        }

        // Draw some text along a line segment.
        // Leave char_num pointing to the next character to be drawn.
        // Leave start_point holding the last point used.
        private static void DrawTextOnSegment(Graphics gr, Brush brush, Font font, string txt, ref int first_ch, ref PointF start_point, PointF end_point, bool text_above_segment)
        {
            float dx = end_point.X - start_point.X;
            float dy = end_point.Y - start_point.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            dx /= dist;
            dy /= dist;
            // See how many characters will fit.
            int last_ch = first_ch;
            while (last_ch < txt.Length)
            {
                string test_string = txt.Substring(first_ch, last_ch - first_ch + 1);
                if (gr.MeasureString(test_string, font).Width > dist)
                {
                    // This is one too many characters.
                    last_ch--;
                    break;
                }
                last_ch++;
            }
            if (last_ch < first_ch) return;
            if (last_ch >= txt.Length) last_ch = txt.Length - 1;
            string chars_that_fit = txt.Substring(first_ch, last_ch - first_ch + 1);

            // Rotate and translate to position the characters.
            GraphicsState state = gr.Save();
            if (text_above_segment)
            {
                gr.TranslateTransform(0,
                    -gr.MeasureString(chars_that_fit, font).Height,
                    MatrixOrder.Append);
            }
            float angle = (float)(180 * Math.Atan2(dy, dx) / Math.PI);
            if (float.IsNaN(angle)) angle = 0;
            gr.RotateTransform(angle, MatrixOrder.Append);
            gr.TranslateTransform(start_point.X, start_point.Y, MatrixOrder.Append);
            // Draw the characters that fit.
            gr.DrawString(chars_that_fit, font, brush, 0, 0);
            // Restore the saved state.
            gr.Restore(state);
            // Update first_ch and start_point.
            first_ch = last_ch + 1;
            float text_width = gr.MeasureString(chars_that_fit, font).Width;
            start_point = new PointF(
                start_point.X + dx * text_width,
                start_point.Y + dy * text_width);
        }

        #endregion Drawing text along lines
    }
}