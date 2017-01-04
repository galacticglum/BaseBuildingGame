using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityUtilities.ObjectPool;

public class MouseController : MonoBehaviour
{
    public Vector2 MousePosition { get { return currentFramePosition; } }

    [SerializeField]
	private GameObject circleCursorPrefab;

	private Vector3 lastFramePosition;
	private Vector3 currentFramePosition;

	private Vector3 dragStartPosition;
	private List<GameObject> dragPreviewGameObjects;

    private bool isDragging;
    private MouseMode currentMouseMode;

    private ConstructionController constructionController;
    private FurnitureGraphicController furnitureGraphicController;

	// Use this for initialization
	private void Start ()
    {
        currentMouseMode = MouseMode.Selection;

        constructionController = FindObjectOfType<ConstructionController>();
        furnitureGraphicController = FindObjectOfType<FurnitureGraphicController>();
        dragPreviewGameObjects = new List<GameObject>();
	}

	// Update is called once per frame
    private void Update ()
    {
		currentFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		currentFramePosition.z = 0;

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            switch (currentMouseMode)
            {
                case MouseMode.Construction:
                    currentMouseMode = MouseMode.Selection;
                    break;
                case MouseMode.Selection:
                    // TODO: Show game menu?
                    break;
            }
        }


        UpdateDragging();
        UpdateCameraMovement();

		lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		lastFramePosition.z = 0;
	}

    private void UpdateDragging()
    {
		if(EventSystem.current.IsPointerOverGameObject()) return;

        // Clean up old drag previews
        while (dragPreviewGameObjects.Count > 0)
        {
            GameObject dragPreviewGameObject = dragPreviewGameObjects[0];
            dragPreviewGameObjects.RemoveAt(0);
            ObjectPool.Destroy(dragPreviewGameObject);
        }

        if (currentMouseMode != MouseMode.Construction) return;

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
        }
        else if (isDragging == false)
        {
            dragStartPosition = currentFramePosition;
        }

        if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.Escape))
        {
            dragStartPosition = currentFramePosition;
            isDragging = false;
        }

        if (constructionController.IsFurnitureDraggable() == false)
        {
            dragStartPosition = currentFramePosition;
        }

        int startX = Mathf.FloorToInt(dragStartPosition.x + 0.5f);
        int endX = Mathf.FloorToInt(currentFramePosition.x + 0.5f);
        int startY = Mathf.FloorToInt(dragStartPosition.y + 0.5f);
        int endY = Mathf.FloorToInt(currentFramePosition.y + 0.5f);

        if (endX < startX)
        {
            int tmp = endX;
            endX = startX;
            startX = tmp;
        }
        if (endY < startY)
        {
            int tmp = endY;
            endY = startY;
            startY = tmp;
        }

        // Display a preview of the drag area
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                Tile tileAt = WorldController.Instance.World.GetTileAt(x, y);
                if (tileAt == null) continue;

                if (constructionController.ConstructionMode == ConstructionMode.Furniture)
                {
                    DrawFurnitureSprite(constructionController.ConstructionObjectType, tileAt);
                }
                else
                {
                    GameObject spawnInstance = ObjectPool.Spawn(circleCursorPrefab, new Vector3(x, y, 0),
                        Quaternion.identity);
                    spawnInstance.transform.SetParent(transform, true);
                    dragPreviewGameObjects.Add(spawnInstance);
                }
            }
        }


        if (!Input.GetMouseButtonUp(0) || !isDragging) return;

        // End Drag
        isDragging = false;
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                Tile tileAt = WorldController.Instance.World.GetTileAt(x, y);

                if (tileAt != null)
                {
                    constructionController.DoBuild(tileAt);
                }
            }
        }
    }

	private void UpdateCameraMovement()
    {
		// Handle screen panning
		if(Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {		
			Vector3 positionDistance = lastFramePosition - currentFramePosition;
			Camera.main.transform.Translate(positionDistance);		
		}

		Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis("Mouse ScrollWheel");
		Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);
	}

    public Tile GetMouseOverTile()
    {
        return WorldController.Instance.GetTileAtWorldCoordinate(currentFramePosition);
    }

    private void DrawFurnitureSprite(string type, Tile tile)
    {
        GameObject spawnInstance = new GameObject();
        spawnInstance.transform.SetParent(transform, true);   
        dragPreviewGameObjects.Add(spawnInstance);

        SpriteRenderer spriteRenderer = spawnInstance.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Jobs";
        spriteRenderer.sprite = furnitureGraphicController.GetSpriteForFurniture(type);
        spriteRenderer.color = World.Current.IsFurniturePlacementValid(type, tile) ?
            new Color(0.5f, 1f, 0.5f, 0.25f) : // Green: placement is valid
            new Color(1f, 0.5f, 0.5f, 0.25f);  // Red: placement is invalid

        Furniture furniturePrototype = World.Current.FurniturePrototypes[type];
        spawnInstance.transform.position = new Vector3(tile.X + (furniturePrototype.Width - 1) / 2f, tile.Y + (furniturePrototype.Height - 1) / 2f, 0);
    }

    public void InConstructionMode()
    {
        dragStartPosition = currentFramePosition;
        isDragging = false;
        currentMouseMode = MouseMode.Construction;
    }
}
