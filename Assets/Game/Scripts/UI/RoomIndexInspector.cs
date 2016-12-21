using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Text))]
public class RoomIndexInspector : MonoBehaviour
{
    private Text text;
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

        text = GetComponent<Text>();
    }

    private void Update()
    {
        Tile tile = mouseController.GetMouseOverTile();
        if (tile == null) return;

        text.text = "Room Index: " + tile.World.Rooms.IndexOf(tile.Room);
    }
}