using System;

namespace ZeroLevel.Services.AsService
{
    [Serializable]
    public enum ValidationResultDisposition
    {
        Success,
        Warning,
        Failure,
    }

    public interface ValidateResult
    {
        /// <summary>
        /// The disposition of the result, any Failure items will prevent
        /// the configuration from completing.
        /// </summary>
        ValidationResultDisposition Disposition { get; }

        /// <summary>
        /// The message associated with the result
        /// </summary>
        string Message { get; }

        /// <summary>
        /// The key associated with the result (chained if configurators are nested)
        /// </summary>
        string Key { get; }

        /// <summary>
        /// The value associated with the result
        /// </summary>
        string Value { get; }
    }
}
