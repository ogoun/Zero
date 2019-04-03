namespace ZeroLevel.Services.PlainTextTables
{
    public enum TextTableStyle
    {
        /// <summary>
        /// No borders
        /// </summary>
        NoBorders,

        /// <summary>
        /// Borders of characters! + -
        /// </summary>
        Simple,

        /// <summary>
        /// Borders of characters +|, only for the first and last row and columns
        /// </summary>
        Columns,

        /// <summary>
        /// Full borders
        /// </summary>
        Borders,

        /// <summary>
        /// Line to separate column names
        /// </summary>
        HeaderLine,

        /// <summary>
        /// Lines for separating column names and first column (row names)
        /// </summary>
        HeaderAndFirstColumn,

        /// <summary>
        /// Full double borders
        /// </summary>
        DoubleBorders,

        DoubleHeaderLine,
        DoubleHeaderAndFirstColumn
    }
}