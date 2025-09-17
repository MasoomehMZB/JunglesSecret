using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    public GameObject gameManagerPrefab;

    private Dictionary<NetworkIdentity, int> playerCharacters = new Dictionary<NetworkIdentity, int>();
    private List<int> availableIndices = new List<int> { 0, 1, 2, 3, 4 };

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
        GameObject playerObj = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        Player player = playerObj.GetComponent<Player>();

        if (player != null)
        {
            player.name = $"Player_{numPlayers + 1}";

            int characterIndex = AssignCharacterToPlayer(player.netIdentity);
            if (characterIndex == -1)
            {
                Debug.LogError("No available characters to assign!");
            }
            else
            {
                player.ServerSetCharacter(characterIndex);

                // Spawn at slot matching the character index
                Vector3 spawnPos = SpawnArea.Instance.GetSpawnPosition(characterIndex);
                playerObj.transform.position = spawnPos;
            }

            GameManager.Instance.players.Add(player);
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
                FreeCharacter(player.netIdentity);
                LobbyUI.Instance.UpdatePlayerReady();
            }
        }

        base.OnServerDisconnect(conn);
    }

    [Server]
    public int AssignCharacterToPlayer(NetworkIdentity player)
    {
        if (availableIndices.Count == 0)
        {
            Debug.LogWarning("No characters left to assign!");
            return -1;
        }

        int randomIndex = Random.Range(0, availableIndices.Count);
        int chosenCharacter = availableIndices[randomIndex];
        availableIndices.RemoveAt(randomIndex);

        playerCharacters[player] = chosenCharacter;
        return chosenCharacter;
    }

    [Server]
    public void FreeCharacter(NetworkIdentity player)
    {
        if (playerCharacters.ContainsKey(player))
        {
            int index = playerCharacters[player];
            availableIndices.Add(index);
            playerCharacters.Remove(player);
        }
    }

}
