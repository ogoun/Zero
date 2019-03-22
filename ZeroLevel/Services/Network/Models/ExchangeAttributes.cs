using System;

namespace ZeroLevel.Network.Microservices
{
    public abstract class ExchangeAttribute : Attribute { }
    /// <summary>
    /// Отмечает метод который является обработчиком сообщений по умолчанию
    /// </summary>
    public sealed class ExchangeMainHandlerAttribute : ExchangeAttribute { }
    /// <summary>
    /// Отмечает метод который является обработчиком запросов по умолчанию
    /// </summary>
    public sealed class ExchangeMainReplierAttribute : ExchangeAttribute { }
    /// <summary>
    /// Отмечает метод-обработчик сообщений для inbox'а с указанным именем
    /// </summary>
    public sealed class ExchangeHandlerAttribute : ExchangeAttribute
    {
        public string Inbox { get; }

        public ExchangeHandlerAttribute(string inbox)
        {
            this.Inbox = inbox;
        }
    }
    /// <summary>
    /// Отмечает метод-обработчик запросов для inbox'а с указанным именем
    /// </summary>
    public sealed class ExchangeReplierAttribute : ExchangeAttribute
    {
        public string Inbox { get; }

        public ExchangeReplierAttribute(string inbox)
        {
            this.Inbox = inbox;
        }
    }
    /// <summary>
    /// Отмечает метод-обработчик сообщений для inbox'а с указанным именем
    /// </summary>
    public sealed class ExchangeMainReplierWithoutArgAttribute : ExchangeAttribute { }
    /// <summary>
    /// Отмечает метод-обработчик запросов для inbox'а с указанным именем
    /// </summary>
    public sealed class ExchangeReplierWithoutArgAttribute : ExchangeAttribute
    {
        public string Inbox { get; }

        public ExchangeReplierWithoutArgAttribute(string inbox)
        {
            this.Inbox = inbox;
        }
    }

    public class ExchangeServerAttribute : Attribute
    {
        public string Protocol { get; }

        public ExchangeServerAttribute(string protocol)
        {
            if (string.IsNullOrWhiteSpace(protocol)) throw new ArgumentNullException(nameof(protocol));
            this.Protocol = protocol;
        }
    }
    public class ExchangeClientAttribute : Attribute
    {
        public string Protocol { get; }

        public ExchangeClientAttribute(string protocol)
        {
            if (string.IsNullOrWhiteSpace(protocol)) throw new ArgumentNullException(nameof(protocol));
            this.Protocol = protocol;
        }
    }
}
