namespace ZeroLevel.Services.Location.WSG84
{
    internal class WSG84Constants
    {
        /// <summary>
        /// Большая полуось (в метрах)
        /// </summary>
        public const float a = 6378137;
        /// <summary>
        /// Полярное сжатие 1/f
        /// </summary>
        public const float f = 298.257223563f;
        /// <summary>
        /// Угловая скорость рад/с-1
        /// </summary>
        public const float w = 7.292115f * .00001f;
        /// <summary>
        /// Геоцентрическая гравитационная постоянная (с учетом массы атмосферы Земли)
        /// </summary>
        public const float GM = 398600.5f;
        /// <summary>
        /// Второй гармонический коэффициент
        /// </summary>
        public const float C20 = -484.16685f * 0.000001f;
        /// <summary>
        /// Нормальный потенциал м2/с2
        /// </summary>
        public const float U0 = 62636861.074f;
        /// <summary>
        /// Скорость света м/с
        /// </summary>
        public const float c = 299792458f;
    }
}
