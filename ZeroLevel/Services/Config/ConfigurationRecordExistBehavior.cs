namespace ZeroLevel.Services.Config
{
    /// <summary>
    /// Merge behavior when keys match
    /// </summary>
    public enum ConfigurationRecordExistBehavior
        : int
    {
        /// <summary>
        /// Add values to existing values
        /// </summary>
        Append = 0,
        /// <summary>
        /// Overwrite existing values
        /// </summary>
        Overwrite = 1,
        /// <summary>
        /// Ignore new values
        /// </summary>
        IgnoreNew = 2
    }
}
