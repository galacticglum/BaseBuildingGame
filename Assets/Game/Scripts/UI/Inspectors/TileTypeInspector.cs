using UnityEngine;
using UnityEngine.UI;

public class TileTypeInspector : MonoBehaviour
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

        string tileType = "Unknown";
        if (mouseOverTile != null)
        {
            tileType = mouseOverTile.Type.ToString();
        }

        text.text = "Tile Type: " + tileType;
    }
}
