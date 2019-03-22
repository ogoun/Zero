using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroLevel.Services.PlainTextTables
{
    public class TextTableRender
    {
        public static string Render(TextTableData data, TextTableRenderOptions options)
        {
            try
            {
                var columns_width = data.
                    Columns.
                    Select(c => c.Width).
                    ToArray();
                var ow = (options.PaddingLeft > 0 ? options.PaddingLeft : 0) +
                    (options.PaddingRight > 0 ? options.PaddingRight : 0);
                for (int i = 0; i < columns_width.Length; i++)
                {
                    columns_width[i] += ow;
                }
                // Обновление ширины столбцов при необходимости
                if (options.MaxCellWidth > 0)
                {
                    for (int i = 0; i < columns_width.Length; i++)
                    {
                        if (columns_width[i] > options.MaxCellWidth)
                        {
                            columns_width[i] = options.MaxCellWidth;
                        }
                    }
                }
                else if (options.MaxTableWidth > 0)
                {
                    int index = 0;
                    while (GetTableWidth(options, columns_width) > options.MaxTableWidth)
                    {
                        for (int i = 0; i < columns_width.Length; i++)
                        {
                            if (columns_width[i] > columns_width[index]) index = i;
                        }
                        columns_width[index]--;
                    }
                }
                var table_width = GetTableWidth(options, columns_width);

                var table = new StringBuilder();
                // Отрисовка таблицы
                var rows = data.Rows.ToArray();
                for (int i = 0; i < rows.Length; i++)
                {
                    DrawRowSeparator(table, options, table_width, i, i == 0, false, columns_width);
                    var row_render = RenderRow(rows[i], options, columns_width);
                    foreach (var line in row_render)
                    {
                        int c = 0;
                        for (; c < columns_width.Length; c++)
                        {
                            DrawColumnSeparator(table, options, c);
                            table.Append(line[c]);
                        }
                        DrawColumnSeparator(table, options, c);
                        table.AppendLine();
                    }
                }
                DrawRowSeparator(table, options, table_width, data.Rows.Count(), false, true, columns_width);
                return table.ToString();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "Сбой при преобразовании таблицы");
                return string.Empty;
            }
        }

        /// <summary>
        /// Рендеринг отображения строки
        /// </summary>
        private static List<string[]> RenderRow(TextTableData.TextTableRow row,
            TextTableRenderOptions options,
            int[] columns_width)
        {
            var result = new List<string[]>();
            // Добавление пустых строк если есть padding сверху
            if (options.PaddingTop > 0)
            {
                for (int i = 0; i < options.PaddingTop; i++)
                {
                    var empty = new string[columns_width.Length];
                    for (int c = 0; c < columns_width.Length; c++)
                    {
                        empty[c] = new string(' ', columns_width[c]);
                    }
                    result.Add(empty);
                }
            }
            // Разделение значений ячеек на части в зависимости от ширины столбца
            var cells = new List<string[]>();
            for (int i = 0; i < columns_width.Length; i++)
            {
                cells.Add(Split(row.Cells[i].Text, columns_width[i], options.PaddingLeft, options.PaddingRight).ToArray());
            }
            // Определение максимального количества строк по ячейкам (высота строки таблицы)
            var max = cells.Max(c => c.Length);
            for (int i = 0; i < max; i++)
            {
                var line = new string[columns_width.Length];
                for (int c = 0; c < columns_width.Length; c++)
                {
                    var text = (cells[c].Length > i) ? cells[c][i] : string.Empty;
                    if (text.Length < columns_width[c])
                    {
                        text = text + new string(' ', columns_width[c] - text.Length);
                    }
                    line[c] = text;
                }
                result.Add(line);
            }
            // Добавление пустых строк если есть padding снизу
            if (options.PaddingBottom > 0)
            {
                for (int i = 0; i < options.PaddingBottom; i++)
                {
                    var empty = new string[columns_width.Length];
                    for (int c = 0; c < columns_width.Length; c++)
                    {
                        empty[c] = new string(' ', columns_width[c]);
                    }
                    result.Add(empty);
                }
            }
            return result;
        }
        /// <summary>
        /// Разделение текстовой строки на подстроки указанной длины
        /// </summary>
        private static IEnumerable<string> Split(string str, int chunkSize, int leftPad, int rightPad)
        {
            if (str == null) return new string[1] { string.Empty };
            while ((chunkSize - (leftPad + rightPad)) < 5 && (leftPad > 0 || rightPad > 0))
            {
                if (leftPad > 0)
                    leftPad--;
                if (rightPad > 0)
                    rightPad--;
            }
            var size = chunkSize - leftPad - rightPad;
            var add = str.Length % size > 0 ? 1 : 0;
            var count = (int)(str.Length / size) + add;
            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                var start = i * size;
                var diff = str.Length - i * size;
                var length = size;
                if (diff < length) length = diff;
                result[i] = (start < str.Length) ? str.Substring(start, length) : string.Empty;
                if (leftPad > 0 && result[i].Length < chunkSize)
                {
                    result[i] = new string(' ', leftPad) + result[i];
                }
                if (rightPad > 0 && result[i].Length < chunkSize)
                {
                    result[i] = result[i] + new string(' ', rightPad);
                }
            }
            return result;
        }
        /// <summary>
        /// Подсчет реальной ширины таблицы, для указанного стиля
        /// </summary>
        private static int GetTableWidth(TextTableRenderOptions options, int[] columns_width)
        {
            int width =
                columns_width.Sum() +   // ширина областей текста
                columns_width.Length - 1;   // границы между ячейками
            switch (options.Style)
            {
                case TextTableStyle.Columns:
                case TextTableStyle.Simple:
                case TextTableStyle.Borders:
                case TextTableStyle.DoubleBorders:
                    width += 2; //  внешние границы
                    break;
            }
            return width;
        }
        /// <summary>
        /// Отрисовка разделителя столбцов
        /// </summary>
        private static void DrawColumnSeparator(StringBuilder sb, TextTableRenderOptions options, int column_index)
        {
            switch (options.Style)
            {
                case TextTableStyle.NoBorders:
                case TextTableStyle.HeaderLine:
                case TextTableStyle.DoubleHeaderLine:
                    sb.Append(' ');
                    break;
                case TextTableStyle.Columns:
                case TextTableStyle.Simple:
                case TextTableStyle.Borders:
                case TextTableStyle.DoubleBorders:
                    sb.Append(options.VerticalLine);
                    break;
                case TextTableStyle.DoubleHeaderAndFirstColumn:
                case TextTableStyle.HeaderAndFirstColumn:
                    if (column_index == 1)
                    {
                        sb.Append(options.VerticalLine);
                    }
                    else
                    {
                        sb.Append(' ');
                    }
                    break;
            }
        }
        /// <summary>
        /// Отрисовка разделителя строк
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="row_index">Индекс строки, имеется в виду индекс следующей строки, т.е. 0 - до отрисовки первой строки</param>
        private static void DrawRowSeparator(StringBuilder sb, TextTableRenderOptions options, int width, int row_index,
            bool isFirst, bool isLast, int[] columns_width)
        {
            var sym = options.HorizontalLine;
            switch (options.Style)
            {
                case TextTableStyle.Columns:
                case TextTableStyle.Simple:
                case TextTableStyle.Borders:
                case TextTableStyle.DoubleBorders:
                    if (isFirst)
                    {
                        sb.Append(options.LeftTopCorner);
                        for (int i = 0; i < columns_width.Length; i++)
                        {
                            sb.Append(options.HorizontalLine, columns_width[i]);
                            if (i != columns_width.Length - 1)
                            {
                                sb.Append(options.HorizontalToDownLine);
                            }
                        }
                        sb.Append(options.RightTopCorner);
                    }
                    else if (isLast)
                    {
                        sb.Append(options.LeftBottomCorner);
                        for (int i = 0; i < columns_width.Length; i++)
                        {
                            sb.Append(options.HorizontalLine, columns_width[i]);
                            if (i != columns_width.Length - 1)
                            {
                                sb.Append(options.HorizontalToUpLine);
                            }
                        }
                        sb.Append(options.RightBottomCorner);
                    }
                    else
                    {
                        sb.Append(options.VerticalToRightLine);
                        for (int i = 0; i < columns_width.Length; i++)
                        {
                            if (options.Style == TextTableStyle.Columns)
                                sb.Append(' ', columns_width[i]);
                            else
                                sb.Append(options.HorizontalLine, columns_width[i]);
                            if (i != columns_width.Length - 1)
                            {
                                sb.Append(options.CrossLines);
                            }
                        }
                        sb.Append(options.VerticalToLeftLine);
                    }
                    break;
                case TextTableStyle.HeaderLine:
                case TextTableStyle.DoubleHeaderLine:
                    if (row_index == 1)
                    {
                        sb.Append(sym, width);
                    }
                    break;
                case TextTableStyle.HeaderAndFirstColumn:
                case TextTableStyle.DoubleHeaderAndFirstColumn:
                    if (row_index == 1)
                    {
                        sb.Append(sym, columns_width[0] + 1);
                        sb.Append(options.CrossLines);
                        sb.Append(sym, width - columns_width[0] - 1);
                    }
                    break;
            }
            sb.AppendLine();
        }
    }
}
