namespace ZeroLevel.Services.Formats.IDX
{
    public class IDXIndex
    {
        private readonly int[] _measures;
        public int[] Cursor { get; private set; }

        public IDXIndex(int[] measures)
        {
            _measures = measures;
            Cursor = new int[_measures.Length];
            Cursor[Cursor.Length - 1] = -1;
        }

        public bool MoveNext()
        {
            Cursor[Cursor.Length - 1]++;
            for (int i = Cursor.Length - 1; i >= 0; i--)
            {
                if (Cursor[i] >= _measures[i])
                {
                    Cursor[i] = 0;
                    if (i > 0)
                    {
                        Cursor[i - 1]++;
                    }
                }
                else
                {
                    return true;
                }
            }
            return false;
        }
    }
}
