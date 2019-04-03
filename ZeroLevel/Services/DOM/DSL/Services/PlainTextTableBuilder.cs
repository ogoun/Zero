using DOM.DSL.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroLevel.DocumentObjectModel.Flow;
using ZeroLevel.Services.PlainTextTables;

namespace DOM.DSL.Services
{
    internal sealed class PlainTextTableBuilder : ISpecialTableBuilder
    {
        private class TextTableMeta
        {
            public TextTableMeta(Column[] columns)
            {
                Data = new TextTableData(columns.Length);
                Data.SetColumnsHeaders(columns.Select(c => c.Caption).ToArray());
            }

            public void FlushRow()
            {
                if (RowCells != null)
                {
                    Data.AppendRow(RowCells);
                    RowCells = null;
                }
            }

            public TextTableData Data;
            public string[] RowCells;
            public int RowCellIndex = -1;

            private StringBuilder _cellBody = new StringBuilder();

            public void FlushCell()
            {
                if (RowCellIndex >= 0)
                {
                    RowCells[RowCellIndex] = _cellBody.ToString();
                    _cellBody.Clear();
                    RowCellIndex = -1;
                }
            }

            public void WriteCell(string part)
            {
                _cellBody.Append(part);
            }

            public string Complete(TextTableRenderOptions options)
            {
                return TextTableRender.Render(Data, options);
            }
        }

        private readonly TextTableRenderOptions _options;
        private Stack<TextTableMeta> _textTables = new Stack<TextTableMeta>();

        public PlainTextTableBuilder(TextTableRenderOptions options)
        {
            _options = options;
        }

        public bool WaitCellBody
        {
            get
            {
                return _textTables.Count > 0 &&
                    _textTables.Peek().RowCellIndex >= 0;
            }
        }

        public void EnterTable(Column[] columns)
        {
            _textTables.Push(new TextTableMeta(columns));
        }

        public void EnterRow(int count_columns)
        {
            _textTables.Peek().RowCells = new string[count_columns];
        }

        public void EnterCell(int order)
        {
            _textTables.Peek().RowCellIndex = order;
        }

        public void LeaveCell()
        {
            _textTables.Peek().FlushCell();
        }

        public void LeaveRow()
        {
            _textTables.Peek().FlushRow();
        }

        public void FlushTable(StringBuilder builder)
        {
            if (_textTables.Count > 0)
            {
                var meta = _textTables.Pop();
                builder.Append(meta.Complete(this._options));
            }
        }

        public void WriteToCell(string part)
        {
            _textTables.Peek().WriteCell(part);
        }
    }
}