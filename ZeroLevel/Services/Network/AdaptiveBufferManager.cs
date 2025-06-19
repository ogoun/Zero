using System;
using System.Buffers;

namespace ZeroLevel.Services.Network
{
    /// <summary>
    /// Управляет адаптивным буфером для сетевых операций
    /// </summary>
    public class AdaptiveBufferManager : IDisposable
    {
        private byte[] _buffer;
        private int _currentSize;
        private readonly int _minSize;
        private readonly int _maxSize;

        // Счетчики для адаптивного изменения размера
        private int _consecutiveSmallReads = 0;
        private int _consecutiveLargeReads = 0;

        // Настройки адаптации
        private readonly double _increaseThreshold;
        private readonly double _decreaseThreshold;
        private readonly int _increaseAfterReads;
        private readonly int _decreaseAfterReads;

        public byte[] Buffer => _buffer;
        public int CurrentSize => _currentSize;

        public AdaptiveBufferManager(
            int minSize = 4096,
            int maxSize = 65536,
            double increaseThreshold = 0.9,
            double decreaseThreshold = 0.25,
            int increaseAfterReads = 3,
            int decreaseAfterReads = 10)
        {
            _minSize = minSize;
            _maxSize = maxSize;
            _currentSize = minSize;
            _increaseThreshold = increaseThreshold;
            _decreaseThreshold = decreaseThreshold;
            _increaseAfterReads = increaseAfterReads;
            _decreaseAfterReads = decreaseAfterReads;

            AllocateBuffer(_currentSize);
        }

        /// <summary>
        /// Обрабатывает результат чтения и адаптирует размер буфера
        /// </summary>
        public void ProcessReadResult(int bytesRead)
        {
            AdjustBufferSize(bytesRead);
        }

        /// <summary>
        /// Принудительно изменить размер буфера
        /// </summary>
        public void ResizeBuffer(int newSize)
        {
            if (newSize < _minSize || newSize > _maxSize)
            {
                throw new ArgumentOutOfRangeException(nameof(newSize),
                    $"Size must be between {_minSize} and {_maxSize}");
            }

            AllocateBuffer(newSize);
        }

        private void AllocateBuffer(int size)
        {
            // Возвращаем старый буфер если есть
            if (_buffer != null)
            {
                ArrayPool<byte>.Shared.Return(_buffer, clearArray: false);
            }

            // Арендуем новый буфер нужного размера
            _buffer = ArrayPool<byte>.Shared.Rent(size);
            _currentSize = _buffer.Length; // ArrayPool может вернуть буфер большего размера
        }

        private void AdjustBufferSize(int bytesRead)
        {
            // Если прочитали почти весь буфер, возможно нужен больший размер
            if (bytesRead >= _currentSize * _increaseThreshold)
            {
                _consecutiveLargeReads++;
                _consecutiveSmallReads = 0;

                // После N больших чтений подряд увеличиваем буфер
                if (_consecutiveLargeReads >= _increaseAfterReads && _currentSize < _maxSize)
                {
                    var newSize = Math.Min(_currentSize * 2, _maxSize);
                    Log.Debug($"[AdaptiveBuffer] Increasing size from {_currentSize} to {newSize}");
                    AllocateBuffer(newSize);
                    _consecutiveLargeReads = 0;
                }
            }
            // Если прочитали мало данных, возможно можно уменьшить буфер
            else if (bytesRead < _currentSize * _decreaseThreshold)
            {
                _consecutiveSmallReads++;
                _consecutiveLargeReads = 0;

                // После N маленьких чтений подряд уменьшаем буфер
                if (_consecutiveSmallReads >= _decreaseAfterReads && _currentSize > _minSize)
                {
                    var newSize = Math.Max(_currentSize / 2, _minSize);
                    Log.Debug($"[AdaptiveBuffer] Decreasing size from {_currentSize} to {newSize}");
                    AllocateBuffer(newSize);
                    _consecutiveSmallReads = 0;
                }
            }
            else
            {
                // Сброс счетчиков при среднем использовании
                _consecutiveSmallReads = 0;
                _consecutiveLargeReads = 0;
            }
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                ArrayPool<byte>.Shared.Return(_buffer, clearArray: false);
                _buffer = null!;
            }
        }
    }
}
