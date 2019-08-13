namespace ZeroLevel.Network
{
    public enum DbgNetworkEvents
        : int
    {
        ServerClientConnected = 0,
        ServerClientDisconnect = 1,
        ClientStartPushRequest = 2,
        ClientCompletePushRequest = 3,
        ClientStartSendResponse = 4,
        ClientCompleteSendResponse = 5,
        ClientStartHandleRequest = 6,
        ClientCompleteHandleRequest = 7,
        ClientGotResponse = 8,
        ClientLostConnection = 9,

        ClientStartSendRequest = 10,
        ClientCompleteSendRequest = 11
    }
}
