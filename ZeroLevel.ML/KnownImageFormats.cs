using System.Collections.Generic;
using System.IO;

namespace ZeroLevel.ML
{
    /// <summary>
    /// Форматы изображений
    /// </summary>
    public static class KnownImageFormats
    {
        private static HashSet<string> _formats = new HashSet<string>() { ".bmp", ".jpeg", ".jpg", ".png", ".tiff", ".webp" };
        /// <summary>
        /// Проверка, является ли файл обрабатываемым приложением как изображение
        /// </summary>
        public static bool IsKnownFormat(string filePath)
        {
            var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ext)) return false;
            return _formats.Contains(ext);
        }
    }
}
