using System;
using System.ComponentModel;

namespace ZeroLevel.DocumentObjectModel
{
    public enum Priority : Int32
    {
        /// <summary>
        /// Обычный
        /// </summary>
        [Description("Normal")]
        Normal = 0,
        /// <summary>
        /// Срочный
        /// </summary>
        [Description("Express")]
        Express = 1,
        /// <summary>
        /// Молния
        /// </summary>
        [Description("Flash")]
        Flash = 2
    }
}
