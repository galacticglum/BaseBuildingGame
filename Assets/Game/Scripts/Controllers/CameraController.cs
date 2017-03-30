using UnityEngine;

public class CameraController
{
    public event CameraMovedEventHandler CameraMoved;
    private void OnCameraMoved()
    {
        CameraMovedEventHandler cameraMoved = CameraMoved;
        if (cameraMoved != null)
        {
            cameraMoved(this, new CameraMovedEventArgs(GetCameraBounds()));
        }
    }

    private float zoomTarget;
    private Vector3 previousCameraPosition;

    // Update is called once per frame.
    public void Update(bool isModal)
    {
        if (isModal)
        {
            return;
        }

        Vector3 oldMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        oldMousePosition.z = 0;

        if (Camera.main.orthographicSize != zoomTarget)
        {
            float target = Mathf.Lerp(Camera.main.orthographicSize, zoomTarget, GameSettings.GetAsInt("ZoomLerp", 3) * Time.deltaTime);
            Camera.main.orthographicSize = Mathf.Clamp(target, 3f, 25f);
        }

        Vector3 newMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        newMousePosition.z = 0;

        Vector3 distance = oldMousePosition - newMousePosition;
        Camera.main.transform.Translate(distance);

        if (previousCameraPosition != Camera.main.transform.position)
        {
            OnCameraMoved();
        }

        previousCameraPosition = Camera.main.transform.position;
    }

    public void Zoom(float amount)
    {
        zoomTarget = Camera.main.orthographicSize - GameSettings.GetAsInt("ZoomSensitivity", 3) * (Camera.main.orthographicSize * amount);
    }

    private static Bounds GetCameraBounds()
    {
        float x = Camera.main.transform.position.x;
        float y = Camera.main.transform.position.y;
        float size = Camera.main.orthographicSize * 2;
        float width = size * Screen.width / Screen.height;
        float height = size;

        return new Bounds(new Vector3(x, y, 0), new Vector3(width, height, 0));
    }
}

