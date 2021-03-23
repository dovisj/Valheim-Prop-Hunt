using GreylingHunt.Transformer;
using HarmonyLib;

namespace GreylingHunt.GameClasses
{

    /// <summary>
    /// Sync server client configuration
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    public static class RPC_PeerInfoPatch
    {
        private static void Postfix(ref ZNet __instance)
        {
            if (!ZNet.m_isServer)
            {
                Log.LogInfo("Asking server what current gamestate is");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "CurrentGameStateSync", new ZPackage());
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_Disconnect")]
    public static class RPC_DisconnectPatch
    {
        private static void Prefix(ref ZRpc rpc)
        {
            PlayerTransformer.Instance.ResetPlayerTransformData(ZNet.instance.GetPeer(rpc).m_characterID);
        }
    }
}
