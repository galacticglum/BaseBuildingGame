using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogBox : MonoBehaviour
{
    private Dictionary<string, object> t;

    public virtual void Show()
    {
        WorldController.Instance.IsModal = true;
        gameObject.SetActive(true);
    }

    public virtual void Close()
    {
        WorldController.Instance.IsModal = false;
        gameObject.SetActive(false);
    }
}
