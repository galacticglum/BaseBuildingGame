using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Cursor
{
    private bool cursorOverride;
    private readonly MouseController mouseController;
    private readonly ConstructionController constructionController;
    private readonly CursorDisplay cursorDisplay;        

    private GameObject cursorObject;
    private SpriteRenderer cursorSpriteRenderer;

    private CursorTextBox upperLeft;
    private CursorTextBox upperRight;
    private CursorTextBox lowerLeft;
    private CursorTextBox lowerRight;

    private readonly Vector3 upperLeftPostion = new Vector3(-64f, 32f, 0);
    private readonly Vector3 upperRightPostion = new Vector3(96f, 32f, 0);
    private readonly Vector3 lowerLeftPostion = new Vector3(-64f, -32f, 0);
    private readonly Vector3 lowerRightPostion = new Vector3(96f, -32f, 0);

    private readonly Vector2 cursorTextBoxSize = new Vector2(120, 50);
    private Texture2D cursorTexture;    
    private readonly GUIStyle style = new GUIStyle();

    private Color characterTint = new Color(1, .7f, 0);
    private Color furnitureTint = new Color(.1f, .7f, .3f);
    private readonly Color defaultTint = Color.white;

    public Cursor(MouseController mouseController, ConstructionController constructionController)
    {
        this.mouseController = mouseController;
        this.constructionController = constructionController;

        cursorDisplay = new CursorDisplay(this.mouseController, this.constructionController);

        style.font = Resources.Load<Font>("Fonts/Arial/Arial");
        style.fontSize = 15;
        
        LoadGraphics();
        GenerateCursor();
    }

    public void Update()
    {
        // Hold Ctrl and press M to activate
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.M))
        {
            cursorOverride = cursorOverride == false;                
        }

        ShowCursor();
        if (cursorOverride)
        {
            return;
        }

        UpdateCursor();
        DisplayCursorInfo();        
    }    

    private void LoadGraphics()
    {
        cursorTexture = Resources.Load<Texture2D>("UI/Cursors/Ship");        
    }

    private void GenerateCursor()
    {
        cursorObject = new GameObject
        {
            name = "CURSOR"
        };

        cursorObject.transform.SetParent(mouseController.CursorParent.transform, true);
        mouseController.CursorParent.name = "Cursor Canvas";

        Canvas cursorCanvas = mouseController.CursorParent.AddComponent<Canvas>();
        cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cursorCanvas.worldCamera = Camera.main;
        cursorCanvas.sortingLayerName = "TileUI";
        cursorCanvas.referencePixelsPerUnit = 411.1f;
        RectTransform rectTransform = mouseController.CursorParent.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 200);

        CanvasScaler canvasScaler = mouseController.CursorParent.AddComponent<CanvasScaler>();
        canvasScaler.dynamicPixelsPerUnit = 100.6f;
        canvasScaler.referencePixelsPerUnit = 411.1f;

        rectTransform = cursorObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(64, 64);
        cursorSpriteRenderer = cursorObject.AddComponent<SpriteRenderer>();
        cursorSpriteRenderer.sortingLayerName = "TileUI";
       
        UnityEngine.Cursor.SetCursor(cursorTexture, new Vector2(0, 0), CursorMode.Auto);
        
        upperLeft = new CursorTextBox(cursorObject, TextAnchor.MiddleRight, style, upperLeftPostion, cursorTextBoxSize);
        upperRight = new CursorTextBox(cursorObject, TextAnchor.MiddleLeft, style, upperRightPostion, cursorTextBoxSize);
        lowerLeft = new CursorTextBox(cursorObject, TextAnchor.MiddleRight, style, lowerLeftPostion, cursorTextBoxSize);
        lowerRight = new CursorTextBox(cursorObject, TextAnchor.MiddleLeft, style, lowerRightPostion, cursorTextBoxSize);        
    }   

    private void UpdateCursor()
    {
        cursorObject.transform.position = Input.mousePosition;       
    }
    
    private void ShowCursor()
    {
        if (EventSystem.current.IsPointerOverGameObject() || cursorOverride == true)
        {            
            cursorObject.SetActive(false);
        }
        else
        {            
            cursorObject.SetActive(true);
        }
    }

    private void DisplayCursorInfo()
    {        
        lowerLeft.Text.text = upperLeft.Text.text = lowerRight.Text.text = upperRight.Text.text = string.Empty;        

        Tile tileAt = WorldController.Instance.GetTileAtWorldCoordinate(mouseController.MousePosition);        
        if (mouseController.MouseMode == MouseMode.Build)
        {
            // Placing furniture object
            switch (constructionController.ConstructionMode)
            {
                case ConstructionMode.Furniture:
                    lowerRight.Text.text = World.Current.FurniturePrototypes[constructionController.ConstructionType].Name;

                    upperLeft.Text.color = Color.green;
                    upperRight.Text.color = Color.red;

                    // Dragging and placing multiple furniture
                    if (tileAt != null && mouseController.IsDragging && mouseController.DragObjects.Count > 1)
                    {
                        cursorDisplay.GetPlacementValidationCounts();
                        upperLeft.Text.text = cursorDisplay.ValidBuildPositionCount();
                        upperRight.Text.text = cursorDisplay.InvalidBuildPositionCount();
                        lowerLeft.Text.text = cursorDisplay.GetCurrentBuildRequirements();
                    }
                    break;
                case ConstructionMode.Floor:
                    lowerRight.Text.text = string.Empty;
                    upperLeft.Text.color = upperRight.Text.color = defaultTint;

                    // Placing tiles and dragging
                    if (tileAt != null && mouseController.IsDragging && mouseController.DragObjects.Count >= 1)
                    {
                        upperLeft.Text.text = mouseController.DragObjects.Count.ToString();
                        lowerLeft.Text.text = constructionController.TileType;
                    }
                    break;
            }
        }
        else
        {
            lowerRight.Text.text = cursorDisplay.MousePosition(tileAt);            
        }        
    }  
}
