using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Text;

class Client
{    
    public string id;
    public Vector2 position;
    public int life;
    public string action;

    public Client(string i, Vector2 p, int l, string a)
    {
        id = i;
        position = p;
        life = l;
        action = a;
    }

    public void UpdateData(Player player)
    {
        position = new Vector2(player.posX, player.posY);
        life = player.life;
        action = player.action;
    }

    public Player ToPlayer()
    {
        Player player = new Player(id, position.x, position.y, life, action);
        return player;
    }
}