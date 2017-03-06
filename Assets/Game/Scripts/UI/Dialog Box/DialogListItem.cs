using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DialogListItem : MonoBehaviour, IPointerClickHandler
{
    public string fileName;
    public InputField inputField;

    public delegate void doubleClickAction();
    public doubleClickAction doubleclick;


    public void OnPointerClick(PointerEventData eventData)
    {
        inputField.text = fileName;
        GameObject deleteButtonObject = GameObject.FindGameObjectWithTag("DeleteButton");

        if (deleteButtonObject != null)
        {
            deleteButtonObject.GetComponent<Image>().color = new Color(255, 255, 255, 255);
            Component text = transform.GetComponentInChildren<Text>();
            GetComponentInParent<DialogBoxLoadGame>().HasPressedDelete = true;
            GetComponentInParent<DialogBoxLoadGame>().FileComponent = text;
        }

        if (eventData.clickCount <= 1) return;
        if (doubleclick != null)
            doubleclick();
    }
}
