using System.Threading;

namespace ZeroLevel.Services
{
    /// <summary>
    /// Класс реализующий потокобезопасный флаг
    /// </summary>
    public sealed class AtomicBoolean
    {
        /// <summary>
        /// Локер для переключения флага указывающего идет или нет процесс обработки
        /// </summary>
        private SpinLock _compareLocker = new SpinLock();
        /// <summary>
        /// Флаг, указывает идет ли в текущий момент процесс обработки очереди
        /// </summary>
        private bool _lock;
        /// <summary>
        /// Потокобезопасное переназначение булевой переменной
        /// функция сранивает переменную со значением comparand
        /// и при совпадении заменяет значение переменной на value
        /// и возвращает true.
        /// При несовпадении значения переменной и comparand
        /// значение переменной останется прежним и функция
        /// вернет false.
        /// </summary>
        /// <param name="target">Переменная</param>
        /// <param name="value">Значение для проставления в случае совпадения</param>
        /// <param name="comparand">Сравниваемое значение</param>
        /// <returns>true - в случае совпадения значений target и comparand</returns>
        private bool CompareExchange(ref bool target, bool value, bool comparand)
        {
            bool lockTaked = false;
            try
            {
                _compareLocker.Enter(ref lockTaked);
                if (target == comparand)
                {
                    target = value;
                    return true;
                }
                return false;
            }
            finally
            {
                if (lockTaked) _compareLocker.Exit();
            }
        }
        /// <summary>
        /// Установка значения в true
        /// </summary>
        /// <returns>true - если значение изменилось, false - если значение занято другим потоком и не изменилось</returns>
        public bool Set()
        {
            return CompareExchange(ref _lock, true, false);
        }
        /// <summary>
        /// Сброс значения
        /// </summary>
        public void Reset()
        {
            CompareExchange(ref _lock, false, true);
        }

        public bool State
        {
            get
            {
                return _lock;
            }
        }
    }
}
