using System;
using System.ComponentModel;

namespace ZeroLevel.Services.Reflection
{
    public static class StringToTypeConverter
    {
        #region TypeHelpers

        /// <summary>
        /// Преобразование строки в тип, если для типа есть соответствующий конвертер, при отсутствии конвертера возвращается
        /// состояние по умолчанию для указанного типа
        /// </summary>
        /// <param name="input">Строка</param>
        /// <param name="to">Тип к которому требуется привести значение в строке</param>
        /// <returns>Результат преобразования</returns>
        public static object TryConvert(string input, Type to)
        {
            try
            {
                return TypeDescriptor.GetConverter(to).ConvertFromString(input);
            }
            catch
            {
            }

            return CreateDefaultState(to);
        }

        /// <summary>
        /// Создание значения по умолчанию для указанного типа
        /// </summary>
        /// <param name="type">Тип</param>
        /// <returns>Значение по умолчанию</returns>
        private static object CreateDefaultState(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        #endregion
    }
}
