using UnityEngine;

namespace GreylingHunt.Utils
{
    public static class Helpers
    {
        public static Character SpawnPrefab(string prefabName, Player player)
        {
            Log.LogInfo("Trying to spawn " + prefabName);
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, prefabName + " does not exist");
                Log.LogInfo("Spawning " + prefabName + " failed");
                return null;
            }
            else
            {
                //If prefab is an npc/enemy 
                if (prefab.GetComponent<Character>())
                {
                    GameObject spawnedChar = Object.Instantiate(prefab, player.transform.position,
                        Quaternion.LookRotation(player.transform.forward));
                    Character character = spawnedChar.GetComponent<Character>();

                    return character;
                }
                else
                {
                    Log.LogInfo("Spawning " + prefabName + " failed, not a Character");
                    return null;
                }
            }
        }

        public static GameObject SpawnPrefab(string prefabName, Vector3 position)
        {
            GameObject spawn = null;
            Log.LogInfo("Trying to spawn " + prefabName);
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Log.LogInfo("Spawning " + prefabName + " failed");
            }
            else
            {
                spawn = GameObject.Instantiate(prefab, position, Quaternion.identity);
                Log.LogInfo("Spawned: " + prefabName);
            }

            return spawn;
        }

        //Server side player IDs
        public static long[] GetZNETPlayerIds()
        {
            var zdos = ZNet.instance.GetAllCharacterZDOS();
            long[] playerIds = new long[zdos.Count];
            int i = 0;
            foreach (var player in zdos)
            {
                playerIds[i] = player.GetLong("playerID");
                i++;
            }

            return playerIds;
        }

        public static ZDOID[] GetPlayerZDOids()
        {
            var zdos = ZNet.instance.GetAllCharacterZDOS();
            ZDOID[] playerZdoids = new ZDOID[zdos.Count];
            int i = 0;
            foreach (var player in zdos)
            {
                playerZdoids[i] = player.m_uid;
                i++;
            }

            return playerZdoids;
        }

        public static Vector3 GetWorldSpawnLocation()
        {
            ZoneSystem.instance.GetLocationIcon("StartTemple", out var pos);
            return pos;
        }
    }
}