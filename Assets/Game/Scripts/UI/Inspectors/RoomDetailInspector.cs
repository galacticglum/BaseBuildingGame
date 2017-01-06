using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class RoomDetailInspector : MonoBehaviour
{
    private Text textComponent;
    private MouseController mouseController;

    private void Start()
    {
        mouseController = FindObjectOfType<MouseController>();
        if (mouseController == null)
        {
            Debug.LogError("RoomDetailInspector::Start: No instance of class: 'MouseController' found!");
            enabled = false;
            return;
        }

        textComponent = GetComponent<Text>();
    }

    private void Update()
    {
        Tile tile = mouseController.GetMouseOverTile();
        if (tile == null || tile.Room == null)
        {
            textComponent.text = "";
            return;
        }

        textComponent.text = tile.Room.GetAllGasses().Aggregate(string.Empty, 
            (current, gas) => current + (gas + ": " + tile.Room.GetGasValue(gas) + " (" + tile.Room.GetGasPercentage(gas) * 100) + "%) ");
    }
}
