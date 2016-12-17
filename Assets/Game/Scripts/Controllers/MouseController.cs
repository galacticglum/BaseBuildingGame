using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityUtilities.ObjectPool;

public class MouseController : MonoBehaviour
{
    public Vector2 MousePosition { get { return MousePosition; } }

    [SerializeField]
    private GameObject circleCursorPrefab;

    private Vector2 lastFramePosition;
    private Vector2 currentFramePosition;

    private Vector2 dragStartPosition;
    private List<GameObject> dragPreviewGameObjects;

    // Use this for initialization
    private void Start()
    {
        dragPreviewGameObjects = new List<GameObject>();
    }

    // Update is called once per frame
    private void Update()
    {
        currentFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        UpdateDragging();
        UpdateCamera();

        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void UpdateDragging()
    {
        // If we're over a UI element, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Start Drag
        if (Input.GetMouseButtonDown(0))
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

        // Call our Roomba, NOW!
        Clean();

        if (Input.GetMouseButton(0))
        {
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    Tile t = WorldController.Instance.World.GetTileAt(x, y);
                    if (t == null) continue;

                    GameObject go = ObjectPool.Spawn(circleCursorPrefab, new Vector2(x, y), Quaternion.identity);
                    go.transform.SetParent(transform, true);
                    dragPreviewGameObjects.Add(go);
                }
            }
        }

        // End Drag
        if (!Input.GetMouseButtonUp(0)) return;
        {
            ConstructionController constructionController = GameObject.FindObjectOfType<ConstructionController>();

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    Tile tile = WorldController.Instance.World.GetTileAt(x, y);

                    if (tile != null)
                    {
                        constructionController.DoBuild(tile);             
                    }
                }
            }
        }
    }

    private void UpdateCamera()
    {
        // Screen panning
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {   
            Vector2 diff = lastFramePosition - currentFramePosition;
            Camera.main.transform.Translate(diff);
        }

        Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis("Mouse ScrollWheel");
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);
    }

    public Tile GetMouseOverTile()
    {
        return WorldController.Instance.GetTileAtWorldCoordinate(currentFramePosition);
    }

    private void Clean()
    {
        while (dragPreviewGameObjects.Count > 0)
        {
            GameObject go = dragPreviewGameObjects[0];
            dragPreviewGameObjects.RemoveAt(0);
            ObjectPool.Destroy(go);
        }
    }
}
