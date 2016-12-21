using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Text))]
public class TileTypeInspector : MonoBehaviour
{
    private Text text;
    private MouseController mouseController;

    private void Start()
    {
        mouseController = FindObjectOfType<MouseController>();
        if (mouseController == null)
        {
            Debug.LogError("TileTypeInspector::Start: No instance of class: 'MouseController' found!");
            enabled = false;
            return;
        }

        text = GetComponent<Text>();
    }

    private void Update()
    {
        Tile tile = mouseController.GetMouseOverTile();
        if (tile == null) return;

        text.text = "Tile Type: " + tile.Type;
    }
}