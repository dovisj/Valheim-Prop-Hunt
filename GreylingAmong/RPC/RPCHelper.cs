namespace GreylingHunt.RPC
{
    public static class RPCHelper
    {
        public static void ServerAnnouncement(long target, string msg)
        {
            bool onScreen = false;
            int screenPos = 1;
            ZRoutedRpc.instance.InvokeRoutedRPC(target, "EventServerAnnouncement", msg,
                onScreen, screenPos);
        }
        public static void ServerAnnouncement(long target, string msg, bool onScreen = true, int screenPos = 1)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(target, "EventServerAnnouncement", msg,
                onScreen, screenPos);
        }
    }
}
