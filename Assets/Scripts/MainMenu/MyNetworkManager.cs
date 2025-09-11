using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class MyNetworkManager : NetworkManager
{
    public GameObject gameManagerPrefab;

    private Color[] playerColors =
 {
        Color.white,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.red,
    };

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (GameManager.Instance == null)
        {
            GameObject gm = Instantiate(gameManagerPrefab);
            NetworkServer.Spawn(gm);   // now server is active 
        }
    }

    // Called when starting as host
    public void StartHostGame()
    {
        StartHost();
        Debug.Log("Host started & advertising");
    }

    // Called when joining by IP
    public void JoinGame(string ip)
    {
        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogWarning("No IP entered.");
            return;
        }

        //networkAddress = ip;
        networkAddress = "localhost";
        StartClient();
        Debug.Log($"Trying to connect to server at: {ip}");
    }

    // Called on server when a new player connects
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Vector3 spawnPos = SpawnArea.Instance.GetSpawnPosition(numPlayers);
        GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity, GameManager.Instance.PlayerParent);

        Player player = playerObj.GetComponent<Player>();
        if (player != null)
        {
            player.name = $"Player_{numPlayers + 1}";

            // Assign unique color
            if (playerColors.Length > 0)
                player.playerColor = playerColors[numPlayers % playerColors.Length];

            // Add to GameManager’s player list
            GameManager.Instance.players.Add(player);
            Debug.Log($"Added{player.netId}, total players = {GameManager.Instance.players.Count}");
            
        }

        // Add to connection
        NetworkServer.AddPlayerForConnection(conn, playerObj);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        GameManager.Instance.InitializeForGameScene();
        GameManager.Instance.SetupBoard();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            Player player = conn.identity.GetComponent<Player>();
            if (player != null)
            {
                GameManager.Instance.players.Remove(player);
                LobbyUI.Instance.UpdatePlayerReady();
            }
        }

        base.OnServerDisconnect(conn);
    }

}
