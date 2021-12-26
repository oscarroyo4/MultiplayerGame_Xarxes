using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkOtherPlayer : MonoBehaviour
{
    int life = 100;
    public Animator anim;
    public RectTransform lifeBar;

    // Start is called before the first frame update
    void Start()
    {
        anim.ResetTrigger("Attack");
    }
    private void Update()
    {
        lifeBar.sizeDelta = new Vector2(life, 10);
    }

    public void Attack()
    {
        anim.SetTrigger("Attack");
    }
    public void Damage(int damage)
    {
        life -= damage;
    }
}
