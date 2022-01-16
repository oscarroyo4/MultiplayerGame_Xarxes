using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerAttack : MonoBehaviour
{
    PlayerController pController;
    private void Start()
    {
        pController = transform.parent.GetComponent<PlayerController>();
    }
    public void Attack()
    {
        pController.Attack();
    }
}
