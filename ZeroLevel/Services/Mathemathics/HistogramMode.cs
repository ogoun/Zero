namespace ZeroLevel.Mathemathics
{
    public enum HistogramMode
    {
        /// <summary>
        /// 1 + 3.2 * Ln(LinksCount)
        /// </summary>
        SQRT,
        /// <summary>
        /// Sqrt(LinksCount)
        /// </summary>
        LOG,
        /// <summary>
        /// Direct count
        /// </summary>
        COUNTS
    }
}