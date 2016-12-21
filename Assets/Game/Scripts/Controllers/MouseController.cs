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

	// Use this for initialization
	private void Start ()
    {
		dragPreviewGameObjects = new List<GameObject>();
	}

	public Tile GetMouseOverTile()
    {
		return WorldController.Instance.GetTileAtWorldCoordinate(currentFramePosition);
	}

	// Update is called once per frame
    private void Update ()
    {
		currentFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		currentFramePosition.z = 0;

		UpdateDragging();
		UpdateCameraMovement();

		lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		lastFramePosition.z = 0;
	}

    private void UpdateDragging()
    {
		if( EventSystem.current.IsPointerOverGameObject())
        {
			return;
		}

		if( Input.GetMouseButtonDown(0) )
        {
			dragStartPosition = currentFramePosition;
		}

		int startX = Mathf.FloorToInt(dragStartPosition.x + 0.5f);
		int endX =   Mathf.FloorToInt(currentFramePosition.x + 0.5f);
		int startY = Mathf.FloorToInt(dragStartPosition.y + 0.5f);
		int endY =   Mathf.FloorToInt(currentFramePosition.y + 0.5f);
		
		if(endX < startX)
        {
			int tmp = endX;
			endX = startX;
			startX = tmp;
		}
		if(endY < startY)
        {
			int tmp = endY;
			endY = startY;
			startY = tmp;
		}

		// Clean up old drag previews
		while(dragPreviewGameObjects.Count > 0)
        {
			GameObject dragPreviewGameObject = dragPreviewGameObjects[0];
			dragPreviewGameObjects.RemoveAt(0);
			ObjectPool.Destroy(dragPreviewGameObject);
		}

		if(Input.GetMouseButton(0))
        {
			// Display a preview of the drag area
			for (int x = startX; x <= endX; x++)
            {
				for (int y = startY; y <= endY; y++)
                {
					Tile tileAt = WorldController.Instance.World.GetTileAt(x, y);
                    if (tileAt == null) continue;

                    GameObject spawnInstance = ObjectPool.Spawn(circleCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                    spawnInstance.transform.SetParent(transform, true);
                    dragPreviewGameObjects.Add(spawnInstance);
                }
			}
		}

		// End Drag
        if (!Input.GetMouseButtonUp(0)) return;
        {
            ConstructionController constructionController = FindObjectOfType<ConstructionController>();

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    Tile tileAt = WorldController.Instance.World.GetTileAt(x, y);

                    if(tileAt != null)
                    {
                        constructionController.DoBuild(tileAt);
                    }
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


}
