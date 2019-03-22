using System;

namespace ZeroLevel.DocumentObjectModel
{
    public enum AssotiationRelation : Int32
    {
        /// <summary>
        /// Тип отношения не определен
        /// </summary>
        Uncknown = 0,
        /// <summary>
        /// Упоминается
        /// </summary>
        Mentions = 1,
        /// <summary>
        /// Рассказывается о
        /// </summary>
        About = 2,
        /// <summary>
        /// Обновление предыдущей версии
        /// </summary>
        UpdateOf = 3,
        /// <summary>
        /// Основано на
        /// </summary>
        BasedOn = 4
    }
}
