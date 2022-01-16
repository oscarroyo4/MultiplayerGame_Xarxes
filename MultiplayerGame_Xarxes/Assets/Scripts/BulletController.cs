using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class BulletController : MonoBehaviour
{
    PhotonView view;
    float bulletTimer = 4.0f;
    public bool destroying = false;
    public int shooterId = 0;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
    }
    public void Destroy()
    {
        if (view.IsMine && !destroying)
        {
            destroying = true;
            PhotonNetwork.Destroy(view);
        }
    }
    private void Update()
    {
        if (bulletTimer <= 0)
        {
            Destroy();
        }
        else
        {
            bulletTimer -= Time.deltaTime;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag != "Player")
        {
            Destroy();
        }
    }
}
