using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Text;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Server : MonoBehaviour
{
    #region "Inspector Members"
    [SerializeField] int port = 8080;
    [Tooltip("Number of frames to wait until next processing")]
    [SerializeField] int frameWait = 2;
    [SerializeField] int maxClients = 2;
    #endregion

    #region "Private Members"
    Socket udp;
    int idAssignIndex = 0;
    Dictionary<EndPoint, Client> clients;
    BinaryFormatter bf = new BinaryFormatter();
    string lastAction;
    #endregion

    void Start()
    {
        clients = new Dictionary<EndPoint, Client>();


        IPAddress ip = IPAddress.Parse("127.0.0.1");
        IPEndPoint endPoint = new IPEndPoint(ip, port);

        Debug.Log("Server IP Address: " + ip);
        Debug.Log("Port: " + port);
        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Bind(endPoint);
        udp.Blocking = false;
    }

    void Update()
    {
        if (Time.frameCount % frameWait == 0 && udp.Available != 0)
        {
            byte[] packet = new byte[1024];
            EndPoint sender = new IPEndPoint(IPAddress.Any, port);

            int rec = udp.ReceiveFrom(packet, ref sender);

            Player player = ByteArrayToObject(packet);

            print(player.id + " " + player.action + " : " + player.posX + " " + player.posY);

            if (string.Compare(player.action, "newClient") == 0 && clients.Count < maxClients)
            {
                HandleNewClient(sender, player);
            }
            else if (string.Compare(player.action, "exit") == 0)
                DisconnectClient(sender, player);
            else if (rec > 0)
            {
                string id = player.id;
                if (string.Compare(player.id, "") == 0) return;

                if(clients[sender].id == player.id) clients[sender].UpdateData(player);
                //SendPositionToAllClients();

                if (string.Compare(player.action, "") != 0)
                {
                    //print(player.action);
                }
            }
            UpdateToAllPlayers();
            lastAction = player.action;
        }
    }

    void HandleNewClient(EndPoint addr, Player data)
    {
        string id = "c" + idAssignIndex++ + "t";
        Debug.Log("Handling new client with id " + id);
        data.id = id;
        //SendPacket(data, addr);

        Vector2 pos = new Vector2(data.posX, data.posY);
        clients.Add(addr, new Client(id, pos, data.life, data.action));
        UpdateToAllPlayers();
    }

    void DisconnectClient(EndPoint sender, Player data)
    {
        Debug.Log("Desconecting client: " + data.id);
        if (clients.ContainsKey(sender))
            clients.Remove(sender);
        Broadcast(data);
    }

    void Broadcast(Player data)
    {
        foreach (KeyValuePair<EndPoint, Client> p in clients)
            SendPacket(data, p.Key);
    }

    void UpdateToAllPlayers()
    {
        foreach (KeyValuePair<EndPoint, Client> p in clients)
            foreach (KeyValuePair<EndPoint, Client> p2 in clients)
            {
                SendPacket(p2.Value.ToPlayer(), p.Key);
            }
    }

    void SendPacket(Player player, EndPoint addr)
    {
        byte[] arr = ObjectToByteArray(player);
        udp.SendTo(arr, addr);
    }

    byte[] ObjectToByteArray(Player data)
    {
        if (data == null)
            return null;

        MemoryStream ms = new MemoryStream();
        bf.Serialize(ms, data);

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
