namespace ZeroLevel.Services.PlainTextTables
{
    public class TextTableRenderOptions
    {
        public int PaddingLeft = 1;
        public int PaddingRight = 1;
        public int PaddingTop = 0;
        public int PaddingBottom = 0;

        private int _maxCellWidth = -1;
        private int _maxTableWidth = -1;

        #region Border kind

        private char SimpleLeftTopCorner = '+';
        private char SimpleRightTopCorner = '+';
        private char SimpleLeftBottomCorner = '+';
        private char SimpleRightBottomCorner = '+';
        private char SimpleVerticalLine = '!';
        private char SimpleHorizontalLine = '-';
        private char SimpleVerticalToLeftLine = '+';
        private char SimpleVerticalToRightLine = '+';
        private char SimpleHorizontalToDownLine = '+';
        private char SimpleHorizontalToUpLine = '+';
        private char SimpleCrossLines = '+';

        private char StandartLeftTopCorner = '┌';
        private char StandartRightTopCorner = '┐';
        private char StandartLeftBottomCorner = '└';
        private char StandartRightBottomCorner = '┘';
        private char StandartVerticalLine = '│';
        private char StandartHorizontalLine = '─';
        private char StandartVerticalToLeftLine = '┤';
        private char StandartVerticalToRightLine = '├';
        private char StandartHorizontalToDownLine = '┬';
        private char StandartHorizontalToUpLine = '┴';
        private char StandartCrossLines = '┼';

        private char DoubleLeftTopCorner = '╔';
        private char DoubleRightTopCorner = '╗';
        private char DoubleLeftBottomCorner = '╚';
        private char DoubleRightBottomCorner = '╝';
        private char DoubleVerticalLine = '║';
        private char DoubleHorizontalLine = '═';
        private char DoubleVerticalToLeftLine = '╣';
        private char DoubleVerticalToRightLine = '╠';
        private char DoubleHorizontalToDownLine = '╦';
        private char DoubleHorizontalToUpLine = '╩';
        private char DoubleCrossLines = '╬';

        private char ColumnLeftTopCorner = '+';
        private char ColumnRightTopCorner = '+';
        private char ColumnLeftBottomCorner = '+';
        private char ColumnRightBottomCorner = '+';
        private char ColumnVerticalLine = '|';
        private char ColumnHorizontalLine = '+';
        private char ColumnVerticalToLeftLine = '|';
        private char ColumnVerticalToRightLine = '|';
        private char ColumnHorizontalToDownLine = '+';
        private char ColumnHorizontalToUpLine = '+';
        private char ColumnCrossLines = '|';

        public char LeftTopCorner =>
            IsStandart ? StandartLeftTopCorner :
                (IsDouble ? DoubleLeftTopCorner :
                    (Style == TextTableStyle.Simple ? SimpleLeftTopCorner :
                        Style == TextTableStyle.Columns ? ColumnLeftTopCorner : ' '));

        public char RightTopCorner =>
            IsStandart ? StandartRightTopCorner :
                (IsDouble ? DoubleRightTopCorner :
                    (Style == TextTableStyle.Simple ? SimpleRightTopCorner :
                        Style == TextTableStyle.Columns ? ColumnRightTopCorner : ' '));

        public char LeftBottomCorner =>
            IsStandart ? StandartLeftBottomCorner :
                (IsDouble ? DoubleLeftBottomCorner :
                    (Style == TextTableStyle.Simple ? SimpleLeftBottomCorner :
                        Style == TextTableStyle.Columns ? ColumnLeftBottomCorner : ' '));

        public char RightBottomCorner =>
            IsStandart ? StandartRightBottomCorner :
                (IsDouble ? DoubleRightBottomCorner :
                    (Style == TextTableStyle.Simple ? SimpleRightBottomCorner :
                        Style == TextTableStyle.Columns ? ColumnRightBottomCorner : ' '));

        public char VerticalLine =>
            IsStandart ? StandartVerticalLine :
                (IsDouble ? DoubleVerticalLine :
                    (Style == TextTableStyle.Simple ? SimpleVerticalLine :
                        Style == TextTableStyle.Columns ? ColumnVerticalLine : ' '));

        public char HorizontalLine =>
            IsStandart ? StandartHorizontalLine :
                (IsDouble ? DoubleHorizontalLine :
                    (Style == TextTableStyle.Simple ? SimpleHorizontalLine :
                        Style == TextTableStyle.Columns ? ColumnHorizontalLine : ' '));

        public char VerticalToLeftLine =>
            IsStandart ? StandartVerticalToLeftLine :
                (IsDouble ? DoubleVerticalToLeftLine :
                    (Style == TextTableStyle.Simple ? SimpleVerticalToLeftLine :
                        Style == TextTableStyle.Columns ? ColumnVerticalToLeftLine : ' '));

        public char VerticalToRightLine =>
            IsStandart ? StandartVerticalToRightLine :
                (IsDouble ? DoubleVerticalToRightLine :
                    (Style == TextTableStyle.Simple ? SimpleVerticalToRightLine :
                        Style == TextTableStyle.Columns ? ColumnVerticalToRightLine : ' '));

        public char HorizontalToDownLine =>
            IsStandart ? StandartHorizontalToDownLine :
                (IsDouble ? DoubleHorizontalToDownLine :
                    (Style == TextTableStyle.Simple ? SimpleHorizontalToDownLine :
                        Style == TextTableStyle.Columns ? ColumnHorizontalToDownLine : ' '));

        public char HorizontalToUpLine =>
            IsStandart ? StandartHorizontalToUpLine :
                (IsDouble ? DoubleHorizontalToUpLine :
                    (Style == TextTableStyle.Simple ? SimpleHorizontalToUpLine :
                        Style == TextTableStyle.Columns ? ColumnHorizontalToUpLine : ' '));

        public char CrossLines =>
            IsStandart ? StandartCrossLines :
                (IsDouble ? DoubleCrossLines :
                    (Style == TextTableStyle.Simple ? SimpleCrossLines :
                        Style == TextTableStyle.Columns ? ColumnCrossLines : ' '));

        #endregion Border kind

        private bool IsStandart => Style == TextTableStyle.Borders ||
            Style == TextTableStyle.HeaderAndFirstColumn ||
            Style == TextTableStyle.HeaderLine;

        private bool IsDouble => Style == TextTableStyle.DoubleBorders ||
            Style == TextTableStyle.DoubleHeaderAndFirstColumn ||
            Style == TextTableStyle.DoubleHeaderLine;

        public int MaxCellWidth
        {
            get
            {
                return _maxCellWidth;
            }
            set
            {
                if (value > 0)
                {
                    _maxTableWidth = -1;
                    if (value > (PaddingLeft + PaddingRight))
                    {
                        _maxCellWidth = value;
                    }
                    else
                    {
                        _maxCellWidth = (PaddingLeft + PaddingRight + 1);
                    }
                }
                else
                {
                    _maxCellWidth = -1;
                }
            }
        }

        public int MaxTableWidth
        {
            get
            {
                return _maxTableWidth;
            }
            set
            {
                if (value > 0)
                {
                    _maxCellWidth = -1;
                    _maxTableWidth = value;
                }
                else
                {
                    _maxTableWidth = -1;
                }
            }
        }

        public TextTableStyle Style = TextTableStyle.Borders;
    }
}