using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Text))]
public class RoomIndexInspector : MonoBehaviour
{
    private Text textComponent;
    private MouseController mouseController;

    private void Start()
    {
        mouseController = FindObjectOfType<MouseController>();
        if (mouseController == null)
        {
            Debug.LogError("RoomIndexInspector::Start: No instance of class: 'MouseController' found!");
            enabled = false;
            return;
        }

        textComponent = GetComponent<Text>();
    }

    private void Update()
    {
        Tile tile = mouseController.GetMouseOverTile();
        if (tile == null) return;

        string index = "N/A";
        if (tile.Room != null)
        {
            index = tile.Room.Index.ToString();
        }

        textComponent.text = "Room Index: " + index;
    }
}