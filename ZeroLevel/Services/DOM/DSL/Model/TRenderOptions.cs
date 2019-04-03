using DOM.DSL.Services;
using System.Linq;
using System.Text;

namespace DOM.DSL.Model
{
    /// <summary>
    /// Feature
    /// </summary>
    internal class TRenderOptions
    {
        public int MaxStringWidth { get; set; } = -1;
        public bool ValidateAsJson { get; set; } = false;
        public bool ValidateAsHtml { get; set; } = false;
        public bool ValidateAsXml { get; set; } = false;
    }

    internal static class TRenderUtils
    {
        public static string SplitOn(string initial, int max)
        {
            var text = new StringBuilder();
            var reader = new TStringReader(initial);
            var current_max = 0;
            while (reader.EOF == false)
            {
                if (char.IsLetterOrDigit(reader.Current))
                {
                    var word = reader.ReadWord();
                    if ((current_max + word.Length) < max)
                    {
                        text.Append(word);
                        current_max += word.Length;
                    }
                    else if (word.Length >= max)
                    {
                        var lines = Enumerable.Range(0, word.Length / max)
                            .Select(i => word.Substring(i * max, max)).
                            ToArray();
                        int k = 0;
                        if (current_max > 0) text.Append("\r\n");
                        for (; k < lines.Length - 1; k++)
                        {
                            text.Append(lines[k]);
                            text.Append("\r\n");
                        }
                        text.Append(lines[k]);
                        current_max = lines[k].Length;
                    }
                    else
                    {
                        text.Append("\r\n");
                        current_max = 0;
                        text.Append(word);
                        current_max = word.Length;
                    }
                    reader.Move(word.Length);
                }
                else if (reader.Current == '\n')
                {
                    current_max = 0;
                    text.Append(reader.Current);
                    reader.Move();
                }
                else
                {
                    text.Append(reader.Current);
                    current_max++;
                    if (current_max >= max)
                    {
                        if (reader.Next == '\r' &&
                            reader.FindOffsetTo('\n') == 2)
                        {
                            text.Append("\r\n");
                            reader.Move(2);
                        }
                        else if (reader.Next != '\n')
                        {
                            text.Append("\r\n");
                        }
                        current_max = 0;
                    }
                    reader.Move();
                }
            }
            return text.ToString();
        }
    }
}