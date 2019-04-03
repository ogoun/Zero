using System;

namespace ZeroLevel.Network.Microservices
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public abstract class ExchangeAttribute : Attribute { }

    /// <summary>
    /// Marks the method that is the default message handler
    /// </summary>
    public sealed class ExchangeMainHandlerAttribute : ExchangeAttribute { }

    /// <summary>
    /// Marks the method that is the default message handler
    /// </summary>
    public sealed class ExchangeMainReplierAttribute : ExchangeAttribute { }

    /// <summary>
    /// Marks a message handler method for an inbox with the specified name.
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
    /// Marks a message handler method for an inbox with the specified name.
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
    /// Marks a message handler method for an inbox with the specified name.
    /// </summary>
    public sealed class ExchangeMainReplierWithoutArgAttribute : ExchangeAttribute { }

    /// <summary>
    /// Marks a message handler method for an inbox with the specified name.
    /// </summary>
    public sealed class ExchangeReplierWithoutArgAttribute : ExchangeAttribute
    {
        public string Inbox { get; }

        public ExchangeReplierWithoutArgAttribute(string inbox)
        {
            this.Inbox = inbox;
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class ExchangeServerAttribute : Attribute
    {
        public string Protocol { get; }

        public ExchangeServerAttribute(string protocol)
        {
            if (string.IsNullOrWhiteSpace(protocol)) throw new ArgumentNullException(nameof(protocol));
            this.Protocol = protocol;
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
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