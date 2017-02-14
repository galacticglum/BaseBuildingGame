using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseController
{
    public SelectionInfo CurrentSelectionInfo { get; private set; }

    public Vector3 MousePosition { get { return currentFramePosition; } }
    public Vector3 PlacingPosition { get; private set; }
    public MouseMode MouseMode { get { return currentMouseMode; } }
    public bool IsDragging { get; private set; }
    public bool IsCharacterSelected { get { return CurrentSelectionInfo != null && CurrentSelectionInfo.IsCharacterSelected; } }

    public List<GameObject> DragObjects { get { return dragPreviewGameObjects; } }
    public Tile MouseOverTile { get { return WorldController.Instance.GetTileAtWorldCoordinate(currentFramePosition); } }
    public GameObject CursorParent { get { return cursorParent; } }

    private readonly GameObject cursorParent;    
    private readonly GameObject circleCursorPrefab;    
    private readonly GameObject furnitureParent;

    private Vector3 lastFramePosition;
    private Vector3 currentFramePosition;

    private Vector3 dragStartPosition;
    private readonly List<GameObject> dragPreviewGameObjects;
    private readonly ConstructionController constructionController;
    private readonly FurnitureGraphicController furnitureGraphicController;
    private readonly ContextMenu contextMenu;
    private readonly Cursor cursor;
    private MenuController menuController;

    private bool isPanning ;

    private const float panningThreshold = 0.015f;
    private Vector3 panningMouseStart = Vector3.zero;
	
    private MouseMode currentMouseMode = global::MouseMode.Selection;

    public MouseController(ConstructionController constructionController, FurnitureGraphicController furnitureGraphicController, GameObject cursorObject)
    {
        this.constructionController = constructionController;
        this.constructionController.MouseController = this;
        this.furnitureGraphicController = furnitureGraphicController;

        circleCursorPrefab = cursorObject;
        menuController = Object.FindObjectOfType<MenuController>();
        contextMenu = Object.FindObjectOfType<ContextMenu>();
        dragPreviewGameObjects = new List<GameObject>();
        cursorParent = new GameObject("Cursor");
        cursor = new Cursor(this, this.constructionController);
        furnitureParent = new GameObject("Furniture Preview Sprites");
    }

    public void StartConstruction()
    {
        currentMouseMode = MouseMode.Build;
    }

    public void StartSpawnMode()
    {
        currentMouseMode = MouseMode.SpawnInventory;
    }

    public void Update(bool isModal)
    {
        if (isModal)
        {
            return;
        }

        UpdateCurrentFramePosition();

        CalculatePlacingPosition();
        CheckModeChanges();
        IsContextMenuActivated();

        cursor.Update();
        UpdateDragging();
        UpdateCameraMovement();
        UpdateSelection();

        if (Settings.getSettingAsBool("DevTools_enabled", false))
        {
            UpdateSpawnClicking();
        }

        StoreFramePosition();
    }


    private void UpdateCurrentFramePosition()
    {
        currentFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currentFramePosition.z = 0;
    }

    private void CheckModeChanges()
    {
        if (!Input.GetKeyUp(KeyCode.Escape) && !Input.GetMouseButtonUp(1)) return;
        switch (currentMouseMode)
        {
            case MouseMode.Build:
                IsDragging = false;
                currentMouseMode = MouseMode.Selection;
                break;
            case MouseMode.SpawnInventory:
                currentMouseMode = MouseMode.Selection;
                break;
        }
    }

    private void IsContextMenuActivated()
    {
        if (!Input.GetKeyUp(KeyCode.Escape) && !Input.GetMouseButtonUp(1)) return;

        if (currentMouseMode != global::MouseMode.Selection) return;
        if (contextMenu == null || MouseOverTile == null) return;
        if (isPanning)
        {
            contextMenu.Close();
        }
        else if (contextMenu != null)
        {
            contextMenu.Open(MouseOverTile);
        }
    }

    private void StoreFramePosition()
    {
        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
    }

    private void CalculatePlacingPosition()
    {
        if (currentMouseMode == MouseMode.Build
            && constructionController.ConstructionMode == ConstructionMode.Furniture
            && World.Current.FurniturePrototypes.ContainsKey(constructionController.ConstructionType)
            && (World.Current.FurniturePrototypes[constructionController.ConstructionType].Width > 1 ||
            World.Current.FurniturePrototypes[constructionController.ConstructionType].Height > 1))
        {
            // If the furniture has af jobSpot set we would like to use that.
            if (World.Current.FurniturePrototypes[constructionController.ConstructionType].WorkPositionOffset.Equals(Vector2.zero) == false)
            {
                PlacingPosition = new Vector3(
                    currentFramePosition.x - World.Current.FurniturePrototypes[constructionController.ConstructionType].WorkPositionOffset.x,
                    currentFramePosition.y - World.Current.FurniturePrototypes[constructionController.ConstructionType].WorkPositionOffset.y,
                    0);
            }
            else
            {   
                // Otherwise we use the center.
                PlacingPosition = new Vector3(
                    currentFramePosition.x - ((World.Current.FurniturePrototypes[constructionController.ConstructionType].Width - 1f) / 2f),
                    currentFramePosition.y - ((World.Current.FurniturePrototypes[constructionController.ConstructionType].Height - 1f) / 2f),
                    0);
            }
        }
        else
        {
            PlacingPosition = currentFramePosition;
        }
    }

    private void UpdateSelection()
    {
        // This handles us left-clicking on furniture or characters to set a SelectionInfo.
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            CurrentSelectionInfo = null;
        }

        if (currentMouseMode != global::MouseMode.Selection)
        {
            return;
        }

        // If we're over a UI element, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            Tile tileUnderMouse = MouseOverTile;
            if (tileUnderMouse != null)
            {
                if (tileUnderMouse.PendingBuildJob != null)
                {
                    tileUnderMouse.PendingBuildJob.CancelJob();
                }
            }
        }

        if (!Input.GetMouseButtonUp(0)) return;
        {
            if (contextMenu != null)
            {
                contextMenu.Close();
            }

            // We just release the mouse button, so that's our queue to update our SelectionInfo.
            Tile tileUnderMouse = MouseOverTile;

            if (tileUnderMouse == null)
            {
                // No valid tile under mouse.
                return;
            }

            if (CurrentSelectionInfo == null || CurrentSelectionInfo.Tile != tileUnderMouse)
            {
                if (CurrentSelectionInfo != null)
                {
                    CurrentSelectionInfo.Selection.IsSelected = false;
                }

                CurrentSelectionInfo = new SelectionInfo(tileUnderMouse);
                CurrentSelectionInfo.Selection.IsSelected = true;
            }
            else
            {
                CurrentSelectionInfo.Selection.IsSelected = false;
                CurrentSelectionInfo.Build();
                CurrentSelectionInfo.SelectNextStuff();
                CurrentSelectionInfo.Selection.IsSelected = true;
            }
        }
    }

    private void UpdateDragging()
    {
        CleanUpDragPreviews();

        if (currentMouseMode != MouseMode.Build)
        {
            return;
        }

        UpdateIsDragging();

        if (IsDragging == false || constructionController.IsFurnitureDraggable() == false)
        {
            dragStartPosition = PlacingPosition;
        }

        MouseDragParameters mouseDragParams = GetDragParameters();
        ShowPreviews(mouseDragParams);

        if (!IsDragging || !Input.GetMouseButtonUp(0)) return;
        IsDragging = false;

        // If we're over a UI element, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Build(mouseDragParams);
    }

    private void CleanUpDragPreviews()
    {
        while (dragPreviewGameObjects.Count > 0)
        {
            GameObject go = dragPreviewGameObjects[0];
            dragPreviewGameObjects.RemoveAt(0);
            SimplePool.Despawn(go);
        }
    }

    private void UpdateIsDragging()
    {
        if (IsDragging && (Input.GetMouseButtonUp(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            IsDragging = false;
        }
        else if (IsDragging == false && Input.GetMouseButtonDown(0))
        {
            IsDragging = true;
        }
    }

    private MouseDragParameters GetDragParameters()
    {
        int startX = Mathf.FloorToInt(dragStartPosition.x + 0.5f);
        int endX = Mathf.FloorToInt(PlacingPosition.x + 0.5f);
        int startY = Mathf.FloorToInt(dragStartPosition.y + 0.5f);
        int endY = Mathf.FloorToInt(PlacingPosition.y + 0.5f);
        return new MouseDragParameters(startX, endX, startY, endY);
    }

    private void ShowPreviews(MouseDragParameters mouseDragParams)
    {
        for (int x = mouseDragParams.StartX; x <= mouseDragParams.EndX; x++)
        {
            for (int y = mouseDragParams.StartY; y <= mouseDragParams.EndY; y++)
            {
                Tile tileAt = WorldController.Instance.World.GetTileAt(x, y);
                if (tileAt == null) continue;

                if (constructionController.ConstructionMode == ConstructionMode.Furniture)
                {
                    Furniture proto = World.Current.FurniturePrototypes[constructionController.ConstructionType];
                    if (PartOfDrag(tileAt, mouseDragParams, proto.DragMode))
                    {
                        DrawFurniture(constructionController.ConstructionType, tileAt);
                    }
                }
                else
                {
                    ShowVisuals(x, y);
                }
            }
        }
    }

    private void ShowVisuals(int x, int y)
    {
        GameObject gameObject = SimplePool.Spawn(circleCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
        gameObject.transform.SetParent(cursorParent.transform, true);
        gameObject.GetComponent<SpriteRenderer>().sprite = SpriteManager.Current.GetSprite("UI", "CursorCircle");

        dragPreviewGameObjects.Add(gameObject);
    }

    private void Build(MouseDragParameters mouseDragParams)
    {
        for (int x = mouseDragParams.StartX; x <= mouseDragParams.EndX; x++)
        {
            for (int y = mouseDragParams.StartY; y <= mouseDragParams.EndY; y++)
            {
                Tile tileAt = WorldController.Instance.World.GetTileAt(x, y);
                if (constructionController.ConstructionMode == ConstructionMode.Furniture)
                {
                    Furniture furniture = World.Current.FurniturePrototypes[constructionController.ConstructionType];
                    if (!PartOfDrag(tileAt, mouseDragParams, furniture.DragMode)) continue;

                    if (tileAt != null)
                    {
                        constructionController.DoBuild(tileAt);
                    }
                }
                else
                {
                    constructionController.DoBuild(tileAt);
                }
            }
        }
    }

    private static bool PartOfDrag(Tile tile, MouseDragParameters mouseDragParams, string dragType)
    {
        switch (dragType)
        {
            case "border":
                return tile.X == mouseDragParams.StartX || tile.X == mouseDragParams.EndX || tile.Y == mouseDragParams.StartY || tile.Y == mouseDragParams.EndY;
            case "path":
                bool withinXBounds = mouseDragParams.StartX <= tile.X && tile.X <= mouseDragParams.EndX;
                bool onPath = tile.Y == mouseDragParams.RawStartY || tile.X == mouseDragParams.RawEndX;
                return withinXBounds && onPath;
            default:
                return true;
        }
    }

    private void UpdateSpawnClicking()
    {
        if (currentMouseMode != MouseMode.SpawnInventory)
        {
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (!Input.GetMouseButtonUp(0)) return;

        Tile tile = MouseOverTile;
        WorldController.Instance.InventoryDebugController.SpawnInventory(tile);
    }

    private void UpdateCameraMovement()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            panningMouseStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            panningMouseStart.z = 0;
        }

        if (!isPanning)
        {
            Vector3 currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            currentMousePosition.z = 0;

            if (Vector3.Distance(panningMouseStart, currentMousePosition) > panningThreshold * Camera.main.orthographicSize)
            {
                isPanning = true;
            }
        }

        if (isPanning && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
        {  
            Vector3 distance = lastFramePosition - currentFramePosition;
            if (distance != Vector3.zero)
            {
                contextMenu.Close();
                Camera.main.transform.Translate(distance);
            }

            if (Input.GetMouseButton(1))
            {
                IsDragging = false;
            }
        }

        if (!Input.GetMouseButton(1) && !Input.GetMouseButton(2))
        {
            isPanning = false;
        }

        if (EventSystem.current.IsPointerOverGameObject() || WorldController.Instance.IsModal)
        {
            return;
        }

        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            WorldController.Instance.CameraController.Zoom(Input.GetAxis("Mouse ScrollWheel"));
        }
        
        UpdateCameraBounds();
    }

    private static void UpdateCameraBounds()
    {
        Vector3 oldPosition = Camera.main.transform.position;

        oldPosition.x = Mathf.Clamp(oldPosition.x, 0, (float) World.Current.Width - 1);
        oldPosition.y = Mathf.Clamp(oldPosition.y, 0, (float) World.Current.Height - 1);

        Camera.main.transform.position = oldPosition;
    }

    private void DrawFurniture(string furnitureType, Tile tile)
    {
        GameObject gameObject = new GameObject();
        gameObject.transform.SetParent(furnitureParent.transform, true);
        dragPreviewGameObjects.Add(gameObject);

        SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Jobs";
        spriteRenderer.sprite = furnitureGraphicController.GetSpriteForFurniture(furnitureType);

        if (WorldController.Instance.World.IsFurniturePlacementValid(furnitureType, tile) &&
            constructionController.IsBuildJobOverlap(tile, furnitureType) == false)
        {
            spriteRenderer.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        }
        else
        {
            spriteRenderer.color = new Color(1f, 0.5f, 0.5f, 0.25f);
        }

        Furniture furniture = World.Current.FurniturePrototypes[furnitureType];
        gameObject.transform.position = new Vector3(tile.X + ((furniture.Width - 1) / 2f), tile.Y + ((furniture.Height - 1) / 2f), 0);
    }    
}
