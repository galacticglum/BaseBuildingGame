using UnityEngine;
using UnityEngine.UI;

public class FurnitureTypeInspector : MonoBehaviour
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

        if (mouseOverTile != null && mouseOverTile.Furniture != null)
        {
            string result = mouseOverTile.Furniture.Name;
            text.text = LocalizationTable.GetLocalization("furniture") + ": " + result;
        }
        else
        {
            text.text = "";
        }
    }
}
