using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Network
{
    public static class FrameBuilder
    {
        public static Frame BuildFrame<T>(T obj, string inbox)
        {
            var frame = Frame.FromPool();
            frame.FrameId = Frame.GetMessageId();
            frame.IsRequest = false;
            frame.Inbox = inbox;
            frame.Payload = MessageSerializer.SerializeCompatible(obj);
            return frame;
        }
        public static Frame BuildFrame(string inbox)
        {
            var frame = Frame.FromPool();
            frame.FrameId = Frame.GetMessageId();
            frame.IsRequest = false;
            frame.Inbox = inbox;
            frame.Payload = null;
            return frame;
        }
        public static Frame BuildRequestFrame<T>(T obj, string inbox)
        {
            var frame = Frame.FromPool();
            frame.FrameId = Frame.GetMessageId();
            frame.IsRequest = true;
            frame.Inbox = inbox;
            frame.Payload = MessageSerializer.SerializeCompatible(obj);
            return frame;
        }
        public static Frame BuildRequestFrame(string inbox)
        {
            var frame = Frame.FromPool();
            frame.FrameId = Frame.GetMessageId();
            frame.IsRequest = true;
            frame.Inbox = inbox;            
            frame.Payload = null;
            return frame;
        }
        public static Frame BuildResponseFrame(object obj, Frame request)
        {
            var frame = Frame.FromPool();
            frame.IsRequest = true;
            frame.FrameId = request.FrameId;
            frame.Inbox = request.Inbox;
            frame.Payload = MessageSerializer.SerializeCompatible(obj);
            return frame;
        }
        public static Frame BuildResponseFrame<T>(T obj, Frame request)
        {
            var frame = Frame.FromPool();
            frame.IsRequest = true;
            frame.FrameId = request.FrameId;
            frame.Inbox = request.Inbox;
            frame.Payload = MessageSerializer.SerializeCompatible(obj);
            return frame;
        }
        public static Frame BuildResponseFrame<T>(T obj, Frame request, string inbox)
        {
            var frame = Frame.FromPool();
            frame.IsRequest = true;
            frame.FrameId = request.FrameId;
            frame.Inbox = inbox;
            frame.Payload = MessageSerializer.SerializeCompatible(obj);
            return frame;
        }
    }
}
