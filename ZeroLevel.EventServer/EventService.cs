using ZeroLevel.EventServer.Model;
using ZeroLevel.Network;
using ZeroLevel.Services.Applications;

namespace ZeroLevel.EventServer
{
    public class EventService
        : BaseZeroService
    {
        public EventService()
        { 
        }

        protected override void StartAction()
        {
            var host = UseHost();
            this.AutoregisterInboxes(host);
            host.OnConnect += Host_OnConnect;
            host.OnDisconnect += Host_OnDisconnect;
        }

        private void Host_OnDisconnect(ISocketClient obj)
        {
            Log.Info($"Client '{obj.Endpoint.Address}:{obj.Endpoint.Port}' disconnected");
        }

        private void Host_OnConnect(IClient obj)
        {
            Log.Info($"Client '{obj.Socket.Endpoint.Address}:{obj.Socket.Endpoint.Port}' connected");
        }

        protected override void StopAction()
        {
        }

        #region Inboxes
        [ExchangeReplier("onetime")]
        public long OneTimeHandler(ISocketClient client, OneTimeEvent e)
        {
            return 0;
        }

        [ExchangeReplier("periodic")]
        public long PeriodicHandler(ISocketClient client, PeriodicTimeEvent e)
        {
            return 0;
        }

        [ExchangeReplier("eventtrigger")]
        public long AfterEventHandler(ISocketClient client, EventAfterEvent e)
        {
            return 0;
        }

        [ExchangeReplier("eventstrigger")]
        public long AfterEventsHandler(ISocketClient client, EventAfterEvents e)
        {
            return 0;
        }
        #endregion
    }
}
