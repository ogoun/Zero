namespace ZeroLevel.Microservices.Contracts
{
    public interface IExchangeService
    {
        string Name { get; }
        string Key { get; }
        string Endpoint { get; }
        string Version { get; }
        string Protocol { get; }
        string Group { get; }
        string Type { get; }
    }
}
