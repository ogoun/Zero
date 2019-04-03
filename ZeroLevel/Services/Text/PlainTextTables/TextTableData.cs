using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.Services.PlainTextTables
{
    public class TextTableData
    {
        #region Classes
        internal class TextTableCell
        {
            private readonly string _text;
            private readonly TextTableColumn _parent_column;

            public string Text { get { return _text; } }

            public TextTableCell(TextTableColumn column, string text)
            {
                this._parent_column = column;
                this._text = text;
                column.UpdateWidth(this);
            }
        }

        internal class TextTableRow
        {
            private readonly TextTableCell[] _cells;
            public TextTableCell[] Cells { get { return _cells; } }

            public TextTableRow(TextTableCell[] cells)
            {
                this._cells = cells;
            }
        }

        internal class TextTableColumn
        {
            private int _width;
            public int Width
            {
                get
                {
                    return _width;
                }
            }

            public TextTableColumn(string title)
            {
                this._width = title.Length;
            }

            public void UpdateWidth(TextTableCell cell)
            {
                if (cell.Text!=null && cell.Text.Length > _width)
                {
                    _width = cell.Text.Length;
                }
            }
        }
        #endregion

        #region Fields
        private readonly TextTableColumn[] _columns;
        private readonly List<TextTableRow> _rows;
        #endregion

        #region Ctor
        public TextTableData(int column_count)
        {
            _columns = new TextTableColumn[column_count];
            _rows = new List<TextTableRow>();
        }
        #endregion

        #region Properties
        internal TextTableColumn[] Columns { get { return _columns; } }
        internal IEnumerable<TextTableRow> Rows { get { return _rows; } }
        #endregion

        #region API
        /// <summary>
        /// Setting column headers
        /// </summary>
        public void SetColumnsHeaders(string[] headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }
            if (headers.Length != _columns.Length)
            {
                throw new InvalidOperationException($"The number of columns ({headers.Length}) does not correspond to the expected ({_columns.Length})");
            }
            for (int i = 0; i < _columns.Length; i++)
            {
                _columns[i] = new TextTableColumn(headers[i]);
            }
            _rows.Insert(0, new TextTableRow(headers.Select((h, i) => new TextTableCell(_columns[i], h)).ToArray()));
        }

        /// <summary>
        /// Adding a value row
        /// </summary>
        public void AppendRow(string[] cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }
            if (cells.Length != _columns.Length)
            {
                throw new InvalidOperationException($"The number of cells ({cells.Length}) does not correspond to the expected ({_columns.Length})");
            }
            _rows.Add(new TextTableRow(cells.Select((c, i) => new TextTableCell(_columns[i], c)).ToArray()));
        }
        #endregion
    }
}
