using DOM.DSL.Contracts;
using System;
using System.Linq;
using ZeroLevel.Services.PlainTextTables;

namespace DOM.DSL.Services
{
    internal static class SpecialTableBuilderFactory
    {
        public static ISpecialTableBuilder CreateSpecialTableBuilder(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return null;
            ISpecialTableBuilder result = null;
            var args = command.Split(',').Select(s => s.Trim()).ToArray();
            switch (args[0])
            {
                case "plaintext":
                    // (Borders, 1-0-1-0, 0, 96)
                    var options = new TextTableRenderOptions();
                    if (args.Length > 1)    // Стиль
                    {
                        if (Enum.TryParse(args[1], out options.Style) == false)
                            options.Style = TextTableStyle.Borders;
                    }
                    if (args.Length > 2)    // Паддинги
                    {
                        var paddings = args[2].Split(' ');
                        int buffer;
                        for (int i = 0; i < paddings.Length; i++)
                        {
                            if (true == int.TryParse(paddings[i].Trim(), out buffer))
                            {
                                switch (i)
                                {
                                    case 0: options.PaddingLeft = buffer; break;
                                    case 1: options.PaddingTop = buffer; break;
                                    case 2: options.PaddingRight = buffer; break;
                                    case 3: options.PaddingBottom = buffer; break;
                                }
                            }
                        }
                    }
                    if (args.Length > 3)    // Ширина ячейки
                    {
                        int buffer;
                        if (true == int.TryParse(args[3].Trim(), out buffer))
                        {
                            options.MaxCellWidth = buffer;
                        }
                    }
                    if (args.Length > 4)    // Ширина таблицы
                    {
                        int buffer;
                        if (true == int.TryParse(args[4].Trim(), out buffer))
                        {
                            options.MaxTableWidth = buffer;
                        }
                    }
                    return new PlainTextTableBuilder(options);
            }
            return result;
        }
    }
}
