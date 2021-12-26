using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public class Player
{
    public string id;
    public float posX;
    public float posY;
    public int life;
    public string action;

    public Player(float pX, float pY, int l, string a)
    {
        posX = pX;
        posY = pY;
        life = l;
        action = a;
    }

    public Player(string i, float pX, float pY, int l, string a)
    {
        id = i;
        posX = pX;
        posY = pY;
        life = l;
        action = a;
    }
}

[RequireComponent(typeof(NetworkClientDisplay))]
public class NetworkClient : MonoBehaviour
{
    // set it to your server address
    [SerializeField] string serverIP = "127.0.0.1";
    [SerializeField] int port = 8080;

    #region "Public Members"
    public string id { get; private set; }
    public int packetNumber { get; private set; }
    public GameObject netPlayerPrefab;
    [HideInInspector] public Vector3 desiredPosition;
    [HideInInspector] public int life;
    [HideInInspector] public string action;
    #endregion

    #region "Private Members"
    Dictionary<string, GameObject> otherClients;
    NetworkClientDisplay otherClientMover;
    NetworkInputSync clientInput;
    Socket udp;
    IPEndPoint endPoint;
    BinaryFormatter bf = new BinaryFormatter();
    #endregion

    void Awake()
    {
        if (serverIP == "")
            Debug.LogError("Server IP Address not set");
        if (port == -1)
            Debug.LogError("Port not set");

        packetNumber = 0;
        desiredPosition = transform.position;
        life = 100;
        action = "";
        otherClientMover = GetComponent<NetworkClientDisplay>();
        clientInput = GetComponent<NetworkInputSync>();
        otherClients = new Dictionary<string, GameObject>();
        endPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Blocking = false;

        // server will reply with a unique id for this user
        SendInitialReqToServer();
    }

    void SendInitialReqToServer()
    {
        Player player = new Player(transform.position.x, transform.position.y, 100, "newClient");
        byte[] packet = ObjectToByteArray(player);
        udp.SendTo(packet, endPoint);
    }

    public void SendPacket(int life, string action)
    {
        if (id == null || id == "")
        {
            Debug.LogError("NOT Connected to server! (hint: start the server and then play the scene again");
            SendInitialReqToServer();
            return;
        }
        Player player = new Player(id, transform.position.x, transform.position.y, life, action);
        byte[] packet = ObjectToByteArray(player);
        udp.SendTo(packet, endPoint);
    }

    void OnApplicationQuit()
    {
        Player player = new Player(id, transform.position.x, transform.position.y, 100, "exit");
        byte[] packet = ObjectToByteArray(player);
        udp.SendTo(packet, endPoint);
        udp.Close();
    }

    void Update()
    {
        if (udp.Available != 0)
        {
            byte[] buffer = new byte[1024];
            udp.Receive(buffer);

            Player data = ByteArrayToObject(buffer);
            string parsedID = data.id;

            if (string.Compare(parsedID, id) != 0 && string.Compare(data.action, "newClient") == 0)
            {
                // server sending the unique id of the client
                id = parsedID;
                this.gameObject.name = id;
                Debug.Log("client ID: " + id);
                return;
            }
            else if (otherClients.ContainsKey(parsedID))
            {
                if(string.Compare(data.action, "exit") == 0)
                {
                    // means parsedID has disconnected
                    otherClientMover.usersToInterpolate.Remove(otherClients[parsedID]);
                    Destroy(otherClients[parsedID]);
                    otherClients.Remove(parsedID);
                    return;
                }
                else if (string.Compare(data.action, "attacking") == 0)
                {
                    // means parsedID has disconnected
                    otherClientMover.usersToInterpolate.Remove(otherClients[parsedID]);
                    Destroy(otherClients[parsedID]);
                    otherClients.Remove(parsedID);
                    return;
                }
            }

            if (string.Compare(parsedID, "") == 0) return;

            Vector2 posInPacket = new Vector2(data.posX, data.posY);
            if (parsedID.Equals(id))
            {
                otherClientMover.usersToInterpolate.Remove(gameObject);
            }
            else if (otherClients.ContainsKey(parsedID))
            {
                otherClientMover.Move(otherClients[parsedID], posInPacket);
                if (string.Compare(data.action, id) == 0)
                {
                    clientInput.Damage(10);
                }
            }
            else if (!parsedID.Equals(id))
                AddOtherClient(parsedID, posInPacket);
        }
    }

    void AddOtherClient(string parsedID, Vector2 pos)
    {
        GameObject go = GameObject.Instantiate(netPlayerPrefab);
        go.name = parsedID;
        go.transform.position = pos;
        otherClients.Add(parsedID, go);
    }

    byte[] ObjectToByteArray(Player obj)
    {
        if (obj == null)
            return null;

        MemoryStream ms = new MemoryStream();
        bf.Serialize(ms, obj);

        return ms.ToArray();
    }

    Player ByteArrayToObject(byte[] data)
    {
        MemoryStream ms = new MemoryStream();
        ms.Write(data, 0, data.Length);
        ms.Seek(0, SeekOrigin.Begin);
        Player obj = (Player)bf.Deserialize(ms);

        return obj;
    }
}
