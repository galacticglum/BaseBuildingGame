using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RoomDetailInspector : MonoBehaviour
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
        if (mouseOverTile == null || mouseOverTile.Room == null)
        {
            text.text = "";
            return;
        }

        string result = mouseOverTile.Room.GasNames.Aggregate("", (current, gasName) => current + 
        string.Format("{0}: ({1}) {2:0.000} atm ({3:0.0}%)\n", gasName, mouseOverTile.Room.GetModifiedGases(gasName), 
        mouseOverTile.Room.GetGasPressure(gasName), mouseOverTile.Room.GetGasFraction(gasName) * 100));

        text.text = result;
    }
}
