using GreylingHunt.Minigames;
using GreylingHunt.Transformer;
using HarmonyLib;
using UnityEngine;

namespace GreylingHunt.GameClasses
{
    [HarmonyPatch(typeof(Player), "RPC_OnDeath")]
    public static class PlayerOnDeathPatch
    {
        private static void Prefix(long sender)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "PlayerDead",
                Player.m_localPlayer.GetPlayerName());
        }
    }

    [HarmonyPatch(typeof(Player), "OnSpawned")]
    public static class PlayerOnSpawnedPatch
    {
        private static void Prefix()
        {
            if (!ZNet.m_isServer)
            {
                if (PlayerTransformer.Instance.GetTransformHistory().Count == 0 &&
                    PlayerTransformer.Instance.transformsSynced == false)
                {
                    Log.LogInfo("Syncing transforms");
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "PlayerTransformSync",
                        Player.m_localPlayer.GetZDOID());
                    PlayerTransformer.Instance.transformsSynced = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), "GetHoverObject")]
    public static class GetHoverObjectPatch
    {
        private static void Postfix(ref GameObject __result)
        {
            if (GameManager.Instance.GameState == GameState.InProgress)
            {
                __result = null;
            }
        }
    }
}