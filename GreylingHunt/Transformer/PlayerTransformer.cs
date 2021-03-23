using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GreylingHunt.Utils;
using UnityEngine;

namespace GreylingHunt.Transformer
{
    public enum TransformItem
    {
        Human,
        Greyling,
        FirTree
    }

    //For server
    public struct TransformHistoryItem
    {
        public ZDOID tPlayerID;
        public string prefab;
    }

    public class PlayerTransformData
    {
        public Animator playerOrigAnimatorRef;

        public FootStep playerFootStep;

        //armature
        public GameObject playerOrigVisual;
        public Transform[] playerOrigFeet;
        public ZSyncAnimation playerZSyncAnimation;
        public CharacterAnimEvent localPlayerAnimEvent;
        public TransformItem currentTransformation;
        public GameObject impostorObjectVisual;
    }

    public sealed class PlayerTransformer
    {
        private int currentPropIndex;
        private string currentPrefab;
        public bool transformsSynced = false;
        private bool initalized;
        private Dictionary<ZDOID, PlayerTransformData> playerTranformDataDictionary;
        private Dictionary<ZDOID, TransformHistoryItem> transformationServerHistory;

        private readonly HashSet<string> specialNpcs = new(){"Greyling"};
        private readonly string[] supportedProps =
        {
            "Greyling", "FirTree", "Rock_3", "Beech_Stub", "Beech1", "Beech_small1", "Birch1", "StatueCorgi",
            "FirTree_small"
        };

        private readonly string GreylingLFoot = "Armature.001/root/l_hip/l_leg1/l_leg2/l_foot";
        private readonly string GreylingRFoot = "Armature.001/root/l_hip/l_leg1/l_leg2/l_foot";

        //Reflection Refs
        private FieldInfo zSycAnimatorRef;
        private FieldInfo foostepAnimatorRef;

        private BindingFlags reflectionFlags =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

        private static PlayerTransformer instance = null;

        private PlayerTransformer()
        {
        }

        public static PlayerTransformer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PlayerTransformer();
                }

                return instance;
            }
        }

        public void Init()
        {
            Log.LogWarning("Initializing PlayerTransformer");
            if (initalized) return;
            playerTranformDataDictionary = new Dictionary<ZDOID, PlayerTransformData>();
            transformationServerHistory = new Dictionary<ZDOID, TransformHistoryItem>();
            //setup reflection refs
            zSycAnimatorRef = typeof(ZSyncAnimation).GetField("m_animator", reflectionFlags);
            foostepAnimatorRef = typeof(FootStep).GetField("m_animator", reflectionFlags);
            initalized = true;
        }

        public void TransformToPrefab(Player player, string prefabName)
        {
            if (supportedProps.Contains(prefabName))
            {
                //NPC later
                if (prefabName == "Greyling")
                {
                    TurnToGreyling(player, prefabName);
                }
                else
                {
                    TurnToProp(player, prefabName);
                }
                currentPrefab = prefabName;
                Log.LogInfo(player.GetPlayerName() + " has turned to " + prefabName);
            }
            else
            {
                Log.LogWarning("Unknown perfab:: " + prefabName + " to transform to");
            }
        }

        private void TurnToProp(Player player, string prefabName)
        {
            GameObject prop = Helpers.SpawnPrefab(prefabName, player.transform.position);
            PlayerTransformData transformData = GetPlayerTransformData(player);
            if (specialNpcs.Contains(currentPrefab))
            {
                RestoreHumanReferences(transformData);
            }
            CleanupOldPropTransform(transformData);

            //hide current player
            transformData.playerOrigVisual.SetActive(false);
            transformData.localPlayerAnimEvent.enabled = false;
            transformData.playerFootStep.enabled = false;
            //Turn into prop

            Transform visualTransform = prop.transform.GetChild(0);
            visualTransform.parent = player.transform;
            CleanupSomeComponents(visualTransform.gameObject);
            transformData.impostorObjectVisual = visualTransform.gameObject;
            ZNetScene.instance.Destroy(prop.gameObject);

            transformData.currentTransformation = TransformItem.FirTree;
            playerTranformDataDictionary[player.GetZDOID()] = transformData;
        }


        private void TurnToGreyling(Player player, string prefabName)
        {
            Character spawnedCharacter = Helpers.SpawnPrefab(prefabName, player);
            PlayerTransformData transformData = GetPlayerTransformData(player);
            CleanupOldPropTransform(transformData);

            //hide current player
            transformData.playerOrigVisual.SetActive(false);
            transformData.localPlayerAnimEvent.enabled = false;

            //Preload greyling stuff
            Animator greylingAnimator = spawnedCharacter.GetComponentInChildren<Animator>();
            spawnedCharacter.transform.parent = player.transform;
            Transform visualTransform = spawnedCharacter.transform.GetChild(1);
            transformData.impostorObjectVisual = visualTransform.gameObject;
            CleanupSomeComponents(visualTransform.gameObject);

            //Transfer player anims on to of greyling
            visualTransform.parent = player.transform;
            ZNetScene.instance.Destroy(spawnedCharacter.gameObject);
            //do reflection
            zSycAnimatorRef.SetValue(transformData.playerZSyncAnimation, greylingAnimator);
            foostepAnimatorRef.SetValue(transformData.playerFootStep, greylingAnimator);

            transformData.playerFootStep.m_feet[0] = transformData.impostorObjectVisual.transform.Find(GreylingLFoot);
            transformData.playerFootStep.m_feet[1] = transformData.impostorObjectVisual.transform.Find(GreylingRFoot);
            transformData.currentTransformation = TransformItem.Greyling;
            playerTranformDataDictionary[player.GetZDOID()] = transformData;
        }

        public void TurnBackToPerson(Player player)
        {
            Log.LogInfo(player.GetPlayerName() + " is turning back to person");
            PlayerTransformData transformData = GetPlayerTransformData(player);
            if (transformData.currentTransformation != TransformItem.Human)
            {
                RestoreHumanReferences(transformData);

                transformData.playerOrigVisual.SetActive(true);
                ZNetScene.instance.Destroy(transformData.impostorObjectVisual);
                transformData.currentTransformation = TransformItem.Human;
                playerTranformDataDictionary[player.GetZDOID()] = transformData;
            }
            else
            {
                Log.LogWarning("Player is already a human!");
            }
        }

        public PlayerTransformData GetPlayerTransformData(Player player)
        {
            if (playerTranformDataDictionary.TryGetValue(player.GetZDOID(),
                out PlayerTransformData playerTransformData))
            {
                Log.LogInfo("Using cached playertransform data");
                return playerTransformData;
            }

            //Store for rollbacking later
            Transform[] localPlayerOldFeet = new Transform[2];
            CharacterAnimEvent playerAnimEvent = player.GetComponentInChildren<CharacterAnimEvent>();
            Animator localPlayerAnimator = player.GetComponentInChildren<Animator>();
            ZSyncAnimation localPlayZSyncAnimation = player.GetComponent<ZSyncAnimation>();
            FootStep localPlayerFootStep = player.GetComponent<FootStep>();
            GameObject localPlayerVisual = player.transform.Find("Visual").gameObject;
            localPlayerOldFeet[0] = localPlayerFootStep.m_feet[0];
            localPlayerOldFeet[1] = localPlayerFootStep.m_feet[1];
            PlayerTransformData newTransformData = new PlayerTransformData()
            {
                playerOrigAnimatorRef = localPlayerAnimator,
                playerFootStep = localPlayerFootStep,
                playerOrigVisual = localPlayerVisual,
                playerOrigFeet = localPlayerOldFeet,
                playerZSyncAnimation = localPlayZSyncAnimation,
                localPlayerAnimEvent = playerAnimEvent
            };
            playerTranformDataDictionary.Add(player.GetZDOID(), newTransformData);
            return newTransformData;
        }

        private void RestoreHumanReferences(PlayerTransformData transformData)
        {
            //restore feetses
            transformData.playerFootStep.m_feet[0] = transformData.playerOrigFeet[0];
            transformData.playerFootStep.m_feet[1] = transformData.playerOrigFeet[1];
            //restore old animators
            zSycAnimatorRef.SetValue(transformData.playerZSyncAnimation, transformData.playerOrigAnimatorRef);
            foostepAnimatorRef.SetValue(transformData.playerFootStep, transformData.playerOrigAnimatorRef);
            transformData.localPlayerAnimEvent.enabled = true;
        }

        //When switching from prop to prop
        private void CleanupOldPropTransform(PlayerTransformData transformData)
        {
            if (transformData.impostorObjectVisual != null)
            {
                ZNetScene.instance.Destroy(transformData.impostorObjectVisual);
            }
        }

        private void CleanupSomeComponents(GameObject transformObject)
        {
            Collider collider = transformObject.GetComponent<Collider>();
            //disable any colliders
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        public void ResetPlayerTransformData(ZDOID zdoid)
        {
            if (playerTranformDataDictionary != null && playerTranformDataDictionary.ContainsKey(zdoid))
            {
                Log.LogInfo("Reseting Player transform data of ID: "+ zdoid);

                if (playerTranformDataDictionary[zdoid].impostorObjectVisual != null)
                {
                    ZNetScene.instance.Destroy(playerTranformDataDictionary[zdoid].impostorObjectVisual);
                }

                RestoreHumanReferences(playerTranformDataDictionary[zdoid]);
                playerTranformDataDictionary.Remove(zdoid);
            }

            if (transformationServerHistory != null && transformationServerHistory.ContainsKey(zdoid))
            {
                transformationServerHistory.Remove(zdoid);
            }
        }

        public void ResetAllPlayerTransformData()
        {
            Log.LogInfo("Reseting All Players transform data");
            if (playerTranformDataDictionary.Count > 0)
            {
                if (playerTranformDataDictionary.TryGetValue(Player.m_localPlayer.GetZDOID(),
                    out PlayerTransformData transformData))
                {
                    RestoreHumanReferences(transformData);
                    if (transformData.impostorObjectVisual != null)
                    {
                        ZNetScene.instance.Destroy(transformData.impostorObjectVisual);
                    }
                }

                playerTranformDataDictionary.Clear();
                transformationServerHistory.Clear();
            }
        }

        public void AddToTransformationHistory(TransformHistoryItem historyItem)
        {
            if (!transformationServerHistory.ContainsKey(historyItem.tPlayerID))
            {
                transformationServerHistory.Add(historyItem.tPlayerID, historyItem);
            }
            else
            {
                transformationServerHistory[historyItem.tPlayerID] = historyItem;
            }
        }

        public Dictionary<ZDOID, TransformHistoryItem> GetTransformHistory()
        {
            return transformationServerHistory;
        }

        public void TranformToNextProp()
        {
            if (currentPropIndex == supportedProps.Length - 1)
            {
                currentPropIndex = 0;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "PlayerTransformRequest",
                Player.m_localPlayer.GetZDOID(), supportedProps[currentPropIndex]);
            currentPropIndex++;
        }
    }
}