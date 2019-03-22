using System;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    /// <summary>
    /// Смещение содержимого в потоке
    /// </summary>
    public enum FlowAlign : Int32
    {
        /// <summary>
        /// Без смещения
        /// </summary>
        None = 0,
        /// <summary>
        /// Смещение к левой части, допускает продолжение потока справа
        /// </summary>
        Left = 1,
        /// <summary>
        /// Смещение к правой части, допускает продолжение потока слева
        /// </summary>
        Right = 2,
        /// <summary>
        /// Выравнивание по центру
        /// </summary>
        Center = 3
    }
}
