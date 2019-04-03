using System.Text;
using ZeroLevel.DocumentObjectModel.Flow;

namespace DOM.DSL.Contracts
{
    public interface ISpecialTableBuilder
    {
        /// <summary>
        /// Indicates that a table cell body entry is expected.
        /// </summary>
        bool WaitCellBody { get; }

        void WriteToCell(string part);

        void EnterTable(Column[] colunmns);

        void EnterRow(int count_columns);

        void EnterCell(int order);

        void LeaveCell();

        void LeaveRow();

        void FlushTable(StringBuilder builder);
    }
}