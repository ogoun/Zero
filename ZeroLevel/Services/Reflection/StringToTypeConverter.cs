using System;
using System.ComponentModel;
using System.Globalization;

namespace ZeroLevel.Services.Reflection
{
    public static class StringToTypeConverter
    {
        #region TypeHelpers

        /// <summary>
        /// Converting a string to a type, if there is a corresponding converter for the type, in the absence of a converter, the default state for the specified type is returned
        /// </summary>
        public static object TryConvert(string input, Type to)
        {
            try
            {
                if (TypeHelpers.IsNumericTypeWithFloating(to))
                {
                    input = input.
                        Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).
                        Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                }
                return TypeDescriptor.GetConverter(to).ConvertFromString(input);
            }
            catch
            {
            }

            return CreateDefaultState(to);
        }

        /// <summary>
        /// Creating default values for specified type
        /// </summary>
        private static object CreateDefaultState(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        #endregion TypeHelpers
    }
}