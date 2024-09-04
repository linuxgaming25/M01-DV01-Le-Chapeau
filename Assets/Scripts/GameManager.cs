using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Stats")]
    [HideInInspector]
    public bool gameEnded = false;          // has the game ended?
    public float timeToWin;                 // time a player needs to hold the hat for in order to win
    public float invincibleDuration;        // how long after a player gets the hat, are they invincible?
    private float hatPickupTime;            // the time the hat was picked up by the current player

    [Header("Players")]
    public string playerPrefabLocation;     // player prefab path in the Resources folder
    public Transform[] spawnPoints;         // array of player spawn points
    [HideInInspector]
    public PlayerController[] players;      // array of all players
    [HideInInspector]
    public int playerWithHat;               // id of the player who currently has the hat
    private int playersInGame;              // number of players currently in the Game scene

    [Header("Components")]
    public PhotonView photonView;

    // instance
    public static GameManager instance;

    void Awake ()
    {
        // set the instance to this script
        instance = this;
    }

    void Start ()
    {
        players = new PlayerController[PhotonNetwork.PlayerList.Length];
        photonView.RPC("ImInGame", RpcTarget.AllBuffered);
    }

    // when a player loads into the game scene - tell everyone
    [PunRPC]
    void ImInGame ()
    {
        playersInGame++;

        // when all the players are in the scene - spawn the players
        if(playersInGame == PhotonNetwork.PlayerList.Length)
            SpawnPlayer();
    }
    
    // spawns a player and initializes it
    void SpawnPlayer ()
    {
        // instantiate the player across the network
        GameObject playerObj = PhotonNetwork.Instantiate(playerPrefabLocation, spawnPoints[Random.Range(0, spawnPoints.Length)].position, Quaternion.identity, 0);

        // get the player script
        PlayerController playerScript = playerObj.GetComponent<PlayerController>();

        // initialize the player
        playerScript.photonView.RPC("Initialize", RpcTarget.All, PhotonNetwork.LocalPlayer);
    }

    // is the player able to take the hat at this current time?
    public bool CanGetHat ()
    {
        if(Time.time > hatPickupTime + invincibleDuration) return true;
        else return false;
    }

    // called when a player hits the hatted player - gives them the hat
    [PunRPC]
    public void GiveHat (int playerId, bool initialGive = false)
    {
        // remove the hat from the currently hatted player
        if(!initialGive)
            GetPlayer(playerWithHat).SetHat(false);

        // give the hat to the new player
        playerWithHat = playerId;
        GetPlayer(playerId).SetHat(true);
        hatPickupTime = Time.time;
    }

    // returns the player who has the requested id
    public PlayerController GetPlayer (int playerId)
    {
        return players.First(x => x.id == playerId);
    }

    // returns the player of the requested GameObject
    public PlayerController GetPlayer (GameObject playerObject)
    {
        return players.First(x => x.gameObject == playerObject);
    }

    // called when a player's held the hat for the winning amount of time
    [PunRPC]
    void WinGame (int playerId)
    {
        gameEnded = true;
        PlayerController player = GetPlayer(playerId);
        GameUI.instance.SetWinText(player.photonPlayer.NickName);

        Invoke("GoBackToMenu", 3.0f);
    }

    // called after the game has been won - navigates back to the Menu scene
    void GoBackToMenu ()
    {
        PhotonNetwork.LeaveRoom();
        NetworkManager.instance.ChangeScene("Menu");
    }
}