using System;

namespace ZeroLevel.Services.Collections
{
    /// <summary>
    /// Collects data while there is capacity and invokes an action after that (batch processing)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Capacitor<T>
            : IDisposable
    {
        private int _index = -1;
        private int _count = 0;
        private readonly T[] _buffer;
        private readonly Action<T[], int> _dischargeAction;

        public int Count => _count;
        public Capacitor(int dischargeValue, Action<T[], int> dischargeAction)
        {
            if (dischargeValue < 1) dischargeValue = 16;
            if (dischargeAction == null!) throw new ArgumentNullException(nameof(dischargeAction));
            _buffer = new T[dischargeValue];
            _dischargeAction = dischargeAction;
        }
        public void Add(T val)
        {
            _index++;
            if (_index >= _buffer.Length)
            {
                _dischargeAction.Invoke(_buffer, _buffer.Length);
                _index = 0;
                _count = 0;
            }
            _buffer[_index] = val;
            _count++;
        }

        public void Discharge()
        {
            if (_count > 0)
            {
                _dischargeAction.Invoke(_buffer, _count);
            }
        }


        public void Dispose()
        {
            if (_count > 0)
            {
                try
                {
                    Discharge();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"[Capacitor.Dispose] Fault discharge in dispose method");
                }
            }
        }
    }
}
