namespace ZeroLevel.Services.Invokation
{
    /// <summary>
    /// Delegate describing the method call
    /// </summary>
    /// <param name="target">The target on which the method is called</param>
    /// <param name="args">Method Arguments</param>
    /// <returns>Result</returns>
    public delegate object Invoker(object target, params object[] args);
}