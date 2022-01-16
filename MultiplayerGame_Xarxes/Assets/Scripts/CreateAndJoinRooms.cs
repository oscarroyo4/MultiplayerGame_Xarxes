using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{

    public InputField createIn;
    public InputField joinIn;
    public Text createErr;
    public Text joinErr;

    public Animation[] anims;

    public SelectCharController selectedCharCont;

    private void Start()
    {
        foreach (Animation a in anims)
        {
            a.Play();
        }
    }

    public void CreateRoom()
    {
        bool succes = false;
        if (createIn.text.Length > 0) succes = PhotonNetwork.CreateRoom(createIn.text);
        else
        {
            createErr.gameObject.SetActive(true);
            Debug.LogWarning("No text in create room input text!");
        }
        if (!succes)
        {
            joinErr.gameObject.SetActive(true);
            joinErr.text = "Error: Room already exists with this name.";
        }
    }

    public void JoinRoom()
    {
        bool succes = false;
        if (joinIn.text.Length > 0) succes = PhotonNetwork.JoinRoom(joinIn.text);
        else
        {
            joinErr.gameObject.SetActive(true);
            Debug.LogWarning("No text in join room input text!");
        }
        if (!succes)
        {
            joinErr.gameObject.SetActive(true);
            joinErr.text = "Error: No room with this name.";
        }
    }

    public override void OnJoinedRoom()
    {
        PlayerPrefs.SetInt("Character", selectedCharCont.GetCharacter());
        PhotonNetwork.LoadLevel("GameScene");
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError(message);
        base.OnCreateRoomFailed(returnCode, message);
    }

    public override void OnCreatedRoom()
    {
        PlayerPrefs.SetInt("Character", selectedCharCont.GetCharacter());
        base.OnCreatedRoom();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError(message);
        base.OnCreateRoomFailed(returnCode, message);
    }
}
