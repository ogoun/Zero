using System.Threading;

namespace ZeroLevel.DataStructures
{
    /// <summary>
    /// https://referencesource.microsoft.com/#System.Web/Util/SafeBitVector32.cs,b90a9ea209d602a4
    /// </summary>
    public struct SafeBit32Vector
    {
        private volatile int _data;

        internal SafeBit32Vector(int data)
        {
            this._data = data;
        }

        internal bool this[int bit]
        {
            get
            {
                int data = _data;
                return (data & bit) == bit;
            }
            set
            {
                for (; ; )
                {
                    int oldData = _data;
                    int newData;
                    if (value)
                    {
                        newData = oldData | bit;
                    }
                    else
                    {
                        newData = oldData & ~bit;
                    }

#pragma warning disable 0420
                    int result = Interlocked.CompareExchange(ref _data, newData, oldData);
#pragma warning restore 0420

                    if (result == oldData)
                    {
                        break;
                    }
                }
            }
        }


        internal bool ChangeValue(int bit, bool value)
        {
            for (; ; )
            {
                int oldData = _data;
                int newData;
                if (value)
                {
                    newData = oldData | bit;
                }
                else
                {
                    newData = oldData & ~bit;
                }

                if (oldData == newData)
                {
                    return false;
                }

#pragma warning disable 0420
                int result = Interlocked.CompareExchange(ref _data, newData, oldData);
#pragma warning restore 0420

                if (result == oldData)
                {
                    return true;
                }
            }
        }
    }
}
