using System.Collections.Generic;
using GreylingHunt.Transformer;
using GreylingHunt.Utils;

namespace GreylingHunt.RPC
{
    class TransformStateSync
    {
        //TODO SERVER SERVER SERVER
        //Prepare in SERVER to send to player
        public static void RPC_PlayerTransformSync(long sender, ZDOID callerZDO)
        {
            if (!ZNet.m_isServer) return;
            Dictionary<ZDOID, TransformHistoryItem> transformHistory = PlayerTransformer.Instance.GetTransformHistory();
            if (transformHistory.Count <= 0) return; //no transfor history
            var playerZdos = Helpers.GetPlayerZDOids();
            Log.LogInfo(transformHistory.Count + " transformations found on server. And " + playerZdos.Length + " players");
            foreach (ZDOID playerZdo in playerZdos)
            {
                if (transformHistory.TryGetValue(playerZdo, out TransformHistoryItem trHist))
                {
                    if (trHist.prefab == "Human") continue;
                    if (playerZdo != callerZDO)
                    {
                        //sync for current player
                        ZRoutedRpc.instance.InvokeRoutedRPC(sender, "PlayerTransformAction", trHist.tPlayerID,
                            trHist.prefab);
                    }
                }
            }
        }

        public static void RPC_PlayerTransformRequest(long sender, ZDOID zdoID, string prefabName)
        {
            Log.LogInfo("RPC_PlayerTransformRequest from " + sender);
            if (!ZNet.m_isServer) return; //Client... skip
            PlayerTransformer.Instance.AddToTransformationHistory(new TransformHistoryItem()
                {prefab = prefabName, tPlayerID = zdoID});
            //some props don't need special invocation because whole prefab, not only visual
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "PlayerTransformAction", zdoID, prefabName);
        }

        public static void RPC_PlayerTransformAction(long sender, ZDOID zdoID, string prefabName)
        {
            if (ZNet.m_isServer) return; //If Server skip

            if (sender == ZRoutedRpc.instance.GetServerPeerID())
            {
                // Confirm our Server is sending the RPC
                Player tPlayer = ZNetScene.instance.FindInstance(zdoID).GetComponent<Player>();
                if (prefabName == "Human")
                {
                    PlayerTransformer.Instance.TurnBackToPerson(tPlayer);
                }
                else
                {
                    PlayerTransformer.Instance.TransformToPrefab(tPlayer, prefabName);
                }
            }
        }
    }
}