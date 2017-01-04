using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Text))]
public class FurnitureTypeInspector : MonoBehaviour
{
    private Text textComponent;
    private MouseController mouseController;

    private void Start()
    {
        mouseController = FindObjectOfType<MouseController>();
        if (mouseController == null)
        {
            Debug.LogError("FurnitureTypeInspector::Start: No instance of class: 'MouseController' found!");
            enabled = false;
            return;
        }

        textComponent = GetComponent<Text>();
    }

    private void Update()
    {
        Tile tile = mouseController.GetMouseOverTile();
        if (tile == null) return;

        textComponent.text = "Furniture Type: " + (tile.Furniture != null ? tile.Furniture.Name : "NULL");
    }
}