using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class SpawnPlayers : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;

    public Text roomText;

    void Start()
    {
        PhotonNetwork.Instantiate(playerPrefab.name, transform.position, Quaternion.identity);
        roomText.text = "Room: " + PhotonNetwork.CurrentRoom.Name;
    }

    public void Disconnect()
    {
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        PhotonNetwork.LoadLevel("LoadingScene");

        base.OnDisconnected(cause);
    }
}
