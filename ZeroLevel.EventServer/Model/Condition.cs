namespace ZeroLevel.EventServer.Model
{
    public enum Condition
         : int
    {
        /// <summary>
        /// В любом случае
        /// </summary>
        None = 0,
        /// <summary>
        /// Если хотя бы одно событие успешно обработано
        /// </summary>
        OneSuccessfull = 1,
        /// <summary>
        /// Если обработаны все события
        /// </summary>
        AllSuccessfull = 2,
        /// <summary>
        /// Если хотя бы одно событие не обработано
        /// </summary>
        AnyFault = 3
    }
}
