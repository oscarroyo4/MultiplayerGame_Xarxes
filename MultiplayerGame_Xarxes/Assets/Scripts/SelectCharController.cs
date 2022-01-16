using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectCharController : MonoBehaviour
{
    public GameObject[] selectables;

    public int initSelected = 0;

    int selected = -1;

    Vector3 normalScale = new Vector3(1, 1, 1);
    Vector3 selectedScale = new Vector3(1.1f, 1.1f, 1.1f);

    void Start()
    {
        Select(initSelected);
    }

    public void Click(int num)
    {
        Select(num);
    }

    void Select(int num)
    {
        if (selected != -1)
        {
            if (selectables[num] == selectables[selected]) return;

            selectables[selected].transform.localScale = normalScale;
        }
        selectables[num].transform.localScale = selectedScale;
        selected = num;
    }

    public int GetCharacter()
    {
        return selected;
    }
}
