using System;
using GreylingHunt.Minigames;
using GreylingHunt.RPC;
using GreylingHunt.RPC.ChatInteractions;
using GreylingHunt.Transformer;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GreylingHunt.GameClasses
{
    [HarmonyPatch(typeof(Game), "Start")]
    public static class Game_Start_Patch
    {
        private static void Prefix()
        {
            //GameState Sync
            ZRoutedRpc.instance.Register("CurrentGameStateSync",
                new Action<long, ZPackage>(GameStateSync.RPC_CurrentGameStateSync)); //Config Sync
            ZRoutedRpc.instance.Register<float, float>("TeleportAction", GameStateSync.RPC_TeleportAction);
            ZRoutedRpc.instance.Register<string>("PlayerDead", GameStateSync.RPC_PlayerDead);

            //Announcement Sync
            ZRoutedRpc.instance.Register<string, bool, int>("RequestServerAnnouncement",
                ServerAnnouncer.RPC_RequestServerAnnouncement); // Our Mock Server Handler
            ZRoutedRpc.instance.Register<string, bool, int>("EventServerAnnouncement",
                ServerAnnouncer.RPC_EventServerAnnouncement); // Our Client Function

            //Transformation Sync
            ZRoutedRpc.instance.Register<ZDOID>("PlayerTransformSync",
                TransformStateSync.RPC_PlayerTransformSync); //Sync when joining later
            ZRoutedRpc.instance.Register<ZDOID, string>("PlayerTransformRequest",
                TransformStateSync.RPC_PlayerTransformRequest); //PlayerTransformRequest
            ZRoutedRpc.instance.Register<ZDOID, string>("PlayerTransformAction",
                TransformStateSync.RPC_PlayerTransformAction); //PlayerTransformAction
            if (!ZNet.m_isServer) //Client
            {
                ZRoutedRpc.instance.Register("BadRequestMsg",
                    new Action<long, ZPackage>(ServerAnnouncer.RPC_BadRequestMsg)); // Our Error Handler
            }

            GameManager.Instance.Init();
            PlayerTransformer.Instance.Init();
        }
    }


    [HarmonyPatch(typeof(Game), "Update")]
    public static class Game_Update_Patch
    {
        private static void Prefix()
        {
            if (Input.GetKeyDown(KeyCode.Keypad6))
            {
                PlayerTransformer.Instance.TranformToNextProp();
            }

            if (Input.GetKeyDown(KeyCode.Keypad5))
            {
                string[] sfx = {"sfx_haldor_yea", "sfx_MeadBurp", "sfx_haldor_laugh"};
                string toplay = sfx[Random.Range(0, sfx.Length - 1)];
                GameObject prefab = ZNetScene.instance.GetPrefab(toplay);

                prefab.GetComponent<ZSFX>().m_minDelay = 0;
                prefab.GetComponent<ZSFX>().m_maxDelay = 0;
                prefab.GetComponent<ZSFX>().m_maxVol = 1;
                prefab.GetComponent<ZSFX>().m_minVol = 1;
                prefab.GetComponent<AudioSource>().maxDistance = 50;
                prefab.GetComponent<AudioSource>().minDistance = 6;
                GameObject.Instantiate(prefab, Player.m_localPlayer.transform.position, Quaternion.identity);
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                if (PlayerTransformer.Instance.GetPlayerTransformData(Player.m_localPlayer).currentTransformation ==
                    TransformItem.Greyling)
                {
                    Log.LogInfo("Turning back into Person");
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "PlayerTransformRequest",
                        Player.m_localPlayer.GetZDOID(), "Human");
                }
                else
                {
                    Log.LogInfo("Turning into Greyling");
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "PlayerTransformRequest",
                        Player.m_localPlayer.GetZDOID(), "Greyling");
                }
            }
        }
    }

    [HarmonyPatch(typeof(Game), "Logout")]
    public static class GameShutdownPatch
    {
        private static void Prefix()
        {
            PlayerTransformer.Instance.ResetAllPlayerTransformData();
        }
    }
}