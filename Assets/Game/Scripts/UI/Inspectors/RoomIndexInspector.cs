using UnityEngine;
using UnityEngine.UI;

public class RoomIndexInspector : MonoBehaviour
{
    private Text text;
    private MouseController mouseController;

    // Use this for initialization
    private void Start()
    {
        text = GetComponent<Text>();
        if (text == null)
        {
            enabled = false;
            return;
        }

        mouseController = WorldController.Instance.MouseController;
    }
	
    // Update is called once per frame
    private void Update()
    {
        Tile mouseOverTile = mouseController.MouseOverTile;

        string roomID = "N/A";
        if (mouseOverTile != null && mouseOverTile.Room != null)
        {
            roomID = mouseOverTile.Room.Index.ToString();
        }

        text.text = "Room Index: " + roomID;
    }
}
