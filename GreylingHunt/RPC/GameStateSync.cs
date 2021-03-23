using GreylingHunt.Minigames;
using GreylingHunt.Transformer;
using UnityEngine;

namespace GreylingHunt.RPC
{
    class GameStateSync
    {
        public static void RPC_CurrentGameStateSync(long sender, ZPackage statePackage)
        {
            if (ZNet.m_isServer && sender != ZRoutedRpc.instance.GetServerPeerID()) //Server
            {
                Log.LogInfo("Player asked for game state");
                statePackage = new ZPackage();
                GameStateData gameStateData = GameManager.Instance.GetGameStateData();
                gameStateData.Serialize(ref statePackage);
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, "CurrentGameStateSync", gameStateData);
            }

            if(ZNet.m_isServer) return;
            if (statePackage != null &&
                statePackage.Size() > 0 &&
                sender == ZRoutedRpc.instance.GetServerPeerID() //Validate the message is from the server and not another client.
            )
            {
                GameStateData gameStateData = new GameStateData();
                gameStateData.Deserialize(ref statePackage);
                GameManager.Instance.SetGameStateData(gameStateData);
            }
        }


        //Client Stuff
        public static void RPC_TeleportAction(long sender, float x, float z)
        {
            if (ZNet.m_isServer) return;
            if (sender != ZRoutedRpc.instance.GetServerPeerID()) return;
            Player localPlayer = Player.m_localPlayer;
            Vector3 pos = new Vector3(x, localPlayer.transform.position.y, z);
            localPlayer.TeleportTo(pos, localPlayer.transform.rotation, distantTeleport: true);
        }


        //For server
        public static void RPC_PlayerDead(long sender, string playerName)
        {
            if (!ZNet.m_isServer) return;
            GameManager.Instance.RemovePlayerFromGame(playerName);
            PlayerTransformer.Instance.ResetPlayerTransformData(Player.m_localPlayer.GetZDOID());
        }
    }
}