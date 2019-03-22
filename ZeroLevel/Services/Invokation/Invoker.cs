namespace ZeroLevel.Services.Invokation
{
    /// <summary>
    /// Делегат описывающий вызов метода
    /// </summary>
    /// <param name="target">Цель на которой вызывается метод</param>
    /// <param name="args">Аргументы метода</param>
    /// <returns>Результат</returns>
    public delegate object Invoker(object target, params object[] args);
}
