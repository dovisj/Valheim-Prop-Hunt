using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GreylingHunt.Configurations;
using GreylingHunt.RPC;
using GreylingHunt.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GreylingHunt.Minigames
{
    public enum PlayerType
    {
        Spectator = 0,
        Seeker = 1,
        Hider = 2
    }

    public enum GameState
    {
        WaitingForPlayers,
        Starting,
        SeekersAreWaiting,
        InProgress,
        Ended
    }

    public class MinigamePlayer
    {
        public PlayerType playerType;
        public ZNetPeer peer;
    }

    public sealed class GameManager
    {
        private static GameManager instance;
        private GameState gameState = GameState.WaitingForPlayers;
        private Dictionary<string, MinigamePlayer> minigamePlayers = new();
        private List<ZNetPeer> seekers = new();
        private List<ZNetPeer> hiders = new();
        private HashSet<string> hiderNameLookup = new();
        private HashSet<string> seekerNameLookup = new();
        public PlayerType LocalPlayerType = PlayerType.Spectator;

        private GameManager()
        {
        }

        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameManager();
                }

                return instance;
            }
        }

        public GameState GameState => gameState;

        public HashSet<string> HiderNameLookup => hiderNameLookup;

        public HashSet<string> SeekerNameLookup => seekerNameLookup;

        public void Init()
        {
            Log.LogInfo("GameManager starting");
            if (!ZNet.m_isServer) return; //Server
            Log.LogInfo("GameManager Started, InvokeReapeting IUpdateGameStatus");
            GreylingHunt.Instance.StartCoroutine(IUpdateGameStatus());
        }

        IEnumerator IUpdateGameStatus()
        {
            while (true)
            {
                // if (!GameStillValid())
                // {
                //     gameState = GameState.WaitingForPlayers;
                // }
                // Log.LogWarning(PlayerTransformer.Instance.GetTransformHistory().Count);
                switch (gameState)
                {
                    case GameState.WaitingForPlayers:
                    {
                        UpdateWaitingForPlayersState();
                        break;
                    }
                    case GameState.Starting:
                    {
                        GreylingHunt.Instance.StartCoroutine(IGameStartupProcedures());
                        gameState = GameState.InProgress;
                        break;
                    }
                    case GameState.InProgress:
                    {
                        //Place players

                        break;
                    }
                }

                if (gameState != GameState.InProgress &&
                    ZNet.instance.GetNrOfPlayers() >= Configuration.Instance.minPlayersToStart)
                {
                    Log.LogInfo("Sending game will start");
                    for (int i = Configuration.Instance.warmupTime; i-- > 0;)
                    {
                        RPCHelper.ServerAnnouncement(ZRoutedRpc.Everybody,
                            $"Game will start in {i}", true, 2);
                        yield return new WaitForSeconds(1);
                    }

                    gameState = GameState.Starting;
                }


                yield return new WaitForSeconds(5f);
            }
        }

        private void UpdateWaitingForPlayersState()
        {
            Log.LogInfo("Sending message to players");
            int playerCount = ZNet.instance.GetNrOfPlayers();
            if (playerCount != Configuration.Instance.minPlayersToStart)
            {
                RPCHelper.ServerAnnouncement(ZRoutedRpc.Everybody,
                    $"Currently {playerCount} are online. Waiting for {Configuration.Instance.minPlayersToStart - playerCount} more",
                    true, 2);
            }
        }

        IEnumerator IGameStartupProcedures()
        {
            //Setup seekers and hiders
            List<ZNetPeer> allPlayers = ZNet.instance.GetConnectedPeers();
            foreach (ZNetPeer peer in allPlayers)
            {
                if (!minigamePlayers.ContainsKey(peer.m_playerName))
                {
                    minigamePlayers.Add(peer.m_playerName,
                        new MinigamePlayer {peer = peer, playerType = PlayerType.Spectator});
                }
                else if (minigamePlayers.TryGetValue(peer.m_playerName, out var existingPlayer))
                {
                    existingPlayer.playerType = PlayerType.Spectator;
                }
            }

            Log.LogInfo("Got " + allPlayers.Count + " players for Setup procedures");
            seekers = SetupRandomSeekers(allPlayers);
            Log.LogInfo("Got " + seekers.Count + " seekers!");
            hiders = SetupHiders(allPlayers, seekers);
            hiderNameLookup = new HashSet<string>(hiders.Select(x => x.m_playerName));
            seekerNameLookup = new HashSet<string>(seekers.Select(x => x.m_playerName));
            Log.LogInfo("Got " + hiders.Count + " hiders!");
            if (hiders.Count == 0)
            {
                Log.LogWarning("Not Enough hiders, waiting for more players!");
                RPCHelper.ServerAnnouncement(ZRoutedRpc.Everybody, "Not Enough hiders, waiting for more players!", true,
                    2);
                gameState = GameState.WaitingForPlayers;
                yield break;
            }

            RPCHelper.ServerAnnouncement(ZRoutedRpc.Everybody, "Hide & Seek will soon commence!", true, 2);
            yield return new WaitForSeconds(3f);
            RPCHelper.ServerAnnouncement(ZRoutedRpc.Everybody, "Hide & Seek will soon commence!", true, 2);
            yield return new WaitForSeconds(3f);
            RPCHelper.ServerAnnouncement(ZRoutedRpc.Everybody, $"Seekers will be: {string.Join(",", seekerNameLookup)}",
                true, 2);
            yield return new WaitForSeconds(3f);
            RPCHelper.ServerAnnouncement(ZRoutedRpc.Everybody,
                $"Hiders will be: {string.Join(",", hiderNameLookup)}. Better get ready!", true,
                2);
            yield return new WaitForSeconds(3f);
            SendOutGroupSpecificMessages();
            yield return new WaitForSeconds(3f);
            SendOutGroupSpecificMessages();
            yield return new WaitForSeconds(3f);
            SendOutGroupSpecificMessages();
            yield return new WaitForSeconds(3f);
            RPCHelper.ServerAnnouncement(ZRoutedRpc.Everybody, $"Good luck!", true, 2);
            GameStateData gameStateData = instance.GetGameStateData();
            ZPackage stateZPackage = new ZPackage();
            gameStateData.Serialize(ref stateZPackage);
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "CurrentGameStateSync", stateZPackage);
            PlacePlayersForGame();
            yield return null;
        }

        private void SendOutGroupSpecificMessages()
        {
            foreach (KeyValuePair<string, MinigamePlayer> keyValuePair in minigamePlayers)
            {
                if (keyValuePair.Value.playerType == PlayerType.Hider)
                {
                    RPCHelper.ServerAnnouncement(keyValuePair.Value.peer.m_uid,
                        $"You have {Configuration.Instance.seekerWaitTime}s to hide! Run!",
                        true, 2);
                }
                else if (keyValuePair.Value.playerType == PlayerType.Seeker)
                {
                    RPCHelper.ServerAnnouncement(keyValuePair.Value.peer.m_uid,
                        $"You will have to wait {Configuration.Instance.seekerWaitTime}s before you start seeking",
                        true, 2);
                }
            }
        }

        private List<ZNetPeer> SetupRandomSeekers(List<ZNetPeer> allPlayers)
        {
            int playerCount = ZNet.instance.GetNrOfPlayers();
            HnSPlayerConfig currentSetup = Configuration.Instance.hnsPlayerConfiguration8;
            if (playerCount <= 3 && playerCount < 5)
            {
                currentSetup = Configuration.Instance.hnsPlayerConfiguration3;
            }
            else if (playerCount >= 5 && playerCount < 8)
            {
                currentSetup = Configuration.Instance.hnsPlayerConfiguration5;
            }
            else if (playerCount >= 8)
            {
                currentSetup = Configuration.Instance.hnsPlayerConfiguration8;
            }

            //PICK random seekers
            List<ZNetPeer> seekers = allPlayers.OrderBy(x => Guid.NewGuid()).Take(currentSetup.seekers)
                .ToList();
            foreach (ZNetPeer seeker in seekers)
            {
                if (minigamePlayers.TryGetValue(seeker.m_playerName, out var minigamePlayer))
                {
                    minigamePlayer.playerType = PlayerType.Seeker;
                }
            }

            return seekers;
        }

        private List<ZNetPeer> SetupHiders(List<ZNetPeer> allPlayers, List<ZNetPeer> seekers)
        {
            List<ZNetPeer> hiders = new List<ZNetPeer>();
            foreach (ZNetPeer player in allPlayers)
            {
                if (seekers.Any(item => item.m_uid == player.m_uid)) continue;
                hiders.Add(player);
            }

            foreach (ZNetPeer hider in hiders)
            {
                if (minigamePlayers.TryGetValue(hider.m_playerName, out var minigamePlayer))
                {
                    minigamePlayer.playerType = PlayerType.Hider;
                }
            }

            return hiders;
        }

        private void PlacePlayersForGame()
        {
            Vector3 hiderSpawnPos = Helpers.GetWorldSpawnLocation();

            foreach (KeyValuePair<string, MinigamePlayer> keyValuePair in minigamePlayers)
            {
                if (keyValuePair.Value.playerType == PlayerType.Hider)
                {
                    float x = (float) (hiderSpawnPos.x + Configuration.Instance.hiderSpawnRadius *
                        Math.Cos(Random.Range(0, 360) * (Math.PI / 180)));
                    float z = (float) (hiderSpawnPos.z + Configuration.Instance.hiderSpawnRadius *
                        Math.Sin(Random.Range(0, 360) * (Math.PI / 180)));
                    ZRoutedRpc.instance.InvokeRoutedRPC(keyValuePair.Value.peer.m_uid, "TeleportAction", x, z);
                }
                else if (keyValuePair.Value.playerType == PlayerType.Seeker)
                {
                    float x = (float) (hiderSpawnPos.x + 2 * Math.Cos(Random.Range(0, 360) * (Math.PI / 180)));
                    float z = (float) (hiderSpawnPos.z + 2 * Math.Cos(Random.Range(0, 360) * (Math.PI / 180)));
                    ZRoutedRpc.instance.InvokeRoutedRPC(keyValuePair.Value.peer.m_uid, "TeleportAction", x, z);
                }
            }
        }

        public void SetGameStateData(GameStateData gameStateData)
        {
            gameState = gameStateData.GameState;
            Log.LogInfo("Got game state: " + gameState);
            seekerNameLookup = gameStateData.seekerNameLookup;
        }

        public GameStateData GetGameStateData()
        {
            GameStateData gameStateData = new GameStateData();
            gameStateData.GameState = gameState;
            gameStateData.hiderNameLookup = hiderNameLookup;
            gameStateData.seekerNameLookup = seekerNameLookup;
            return gameStateData;
        }

        public bool IsSeeker(string playerName)
        {
            if (minigamePlayers == null) return false;
            if (minigamePlayers.TryGetValue(playerName, out var minigamePlayer))
            {
                return minigamePlayer.playerType == PlayerType.Seeker;
            }

            return false;
        }

        public bool IsHider(string playerName)
        {
            if (minigamePlayers == null) return false;
            if (minigamePlayers.TryGetValue(playerName, out var minigamePlayer))
            {
                return minigamePlayer.playerType == PlayerType.Hider;
            }

            return false;
        }

        public void RemovePlayerFromGame(string playerName)
        {
            if (minigamePlayers.TryGetValue(playerName, out var minigamePlayer))
            {
                if (minigamePlayer.playerType == PlayerType.Hider)
                {
                    minigamePlayer.playerType = PlayerType.Spectator;
                    hiderNameLookup.Remove(playerName);
                }
                else if (minigamePlayer.playerType == PlayerType.Seeker)
                {
                    seekerNameLookup.Remove(playerName);
                }
            }
        }

        private bool GameStillValid()
        {
            if (seekerNameLookup.Count == 0) return false;
            if (hiderNameLookup.Count == 0) return false;
            return true;
        }
    }
}