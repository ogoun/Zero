namespace ZeroLevel.Qdrant
{
    public static class DoubleExtensions
    {
        public static string ConvertToString(this double num)
        {
            return num.ToString().Replace(',', '.');
        }
    }
}
