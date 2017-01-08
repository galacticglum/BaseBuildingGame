using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogBoxListItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private InputField inputField;
    public InputField InputField
    {
        get { return inputField; }
        set { inputField = value; }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        InputField.text = transform.GetComponentInChildren<Text>().text;
    }
}
