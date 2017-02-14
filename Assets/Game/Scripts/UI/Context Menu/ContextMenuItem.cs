using UnityEngine;
using UnityEngine.UI;

public class ContextMenuItem : MonoBehaviour
{
    [SerializeField]
    private ContextMenu contextMenu;
    public ContextMenu ContextMenu
    {
        get { return contextMenu; }
        set { contextMenu = value; }
    }

    public ContextMenuAction Action { get; set; }

    [SerializeField]
    private Text text;
    private MouseController mouseController;

    public void Start()
    {
        mouseController = WorldController.Instance.MouseController;
    }

    public void BuildInterface()
    {
        text.text = Action.Text;
    }

    public void OnClick()
    {
        if (Action != null)
        {
            Action.OnClick(mouseController);
        }

        if (contextMenu != null)
        {
            contextMenu.Close();
        }
    }
}