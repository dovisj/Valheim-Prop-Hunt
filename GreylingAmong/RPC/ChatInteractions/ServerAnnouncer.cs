namespace GreylingHunt.RPC.ChatInteractions
{
    public class ServerAnnouncer
    {
        public static void RPC_EventServerAnnouncement(long sender, string msg, bool onScreen, int messagePos)
        {

            if (ZNet.m_isServer) return; //If Server skip
            if (Player.m_localPlayer == null || Player.m_localPlayer.m_nview == null || !(bool)MessageHud.instance) return;
            if (sender == ZRoutedRpc.instance.GetServerPeerID() && msg.Length > 0)
            { // Confirm our Server is sending the RPC
                if (onScreen)
                {
                    Player.m_localPlayer.Message((MessageHud.MessageType)messagePos, msg);
                }
                else
                {
                    Chat.instance.AddString("Server", msg, Talker.Type.Shout); // Add our server announcement to the Client's chat instance
                }
            }
        }

        public static void RPC_BadRequestMsg(long sender, ZPackage pkg)
        {

            Log.LogInfo("Got badrequest");
            if (sender == ZRoutedRpc.instance.GetServerPeerID() && pkg != null && pkg.Size() > 0)
            { // Confirm our Server is sending the RPC
                string msg = pkg.ReadString(); // Get Our Msg
                if (msg != "")
                { // Make sure it isn't empty
                    Chat.instance.AddString("Server", "<color=\"red\">" + msg + "</color>", Talker.Type.Normal); // Add to chat with red color because it's an error
                }
            }
        }

        public static void RPC_RequestServerAnnouncement(long sender, string msg, bool onScreen, int messagePos)
        {
            if (!ZNet.m_isServer) return; //Client... skip
            Log.LogInfo("Requesting announcement!");

            if (msg.Length > 0)
            { // Check that our Package is not null, and if it isn't check that it isn't empty.
                ZNetPeer peer = ZNet.instance.GetPeer(sender); // Get the Peer from the sender, to later check the SteamID against our Adminlist.

                if (peer != null)
                { // Confirm the peer exists
                    string peerSteamID = ((ZSteamSocket)peer.m_socket).GetPeerID().m_SteamID.ToString(); // Get the SteamID from peer.
                    if ( ZNet.instance.m_adminList != null && ZNet.instance.m_adminList.Contains(peerSteamID))
                    { // Check that the SteamID is in our Admin List.
                        Log.LogInfo("is Admin");
                        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "EventServerAnnouncement", msg, onScreen, messagePos); // Send our Event to all Clients. 0L specifies that it will be sent to everybody
                    }
                    else
                    {
                        Log.LogInfo("not Admin");
                        ZPackage newPkg = new ZPackage(); // Create a new ZPackage.
                        newPkg.Write("You aren't an Admin!"); // Tell them what's going on.
                        ZRoutedRpc.instance.InvokeRoutedRPC(sender, "BadRequestMsg", new object[] { newPkg }); // Send the error message.
                    }
                }
                else
                {
                    Log.LogInfo("you are not admin");
                    ZPackage newPkg = new ZPackage(); // Create a new ZPackage.
                    newPkg.Write("You aren't an Admin!"); // Tell them what's going on.
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, "BadRequestMsg", new object[] { newPkg }); // Send the error message.
                }
            }
            else
            {
                Log.LogInfo("Empty package");
            }

        }
    }
}