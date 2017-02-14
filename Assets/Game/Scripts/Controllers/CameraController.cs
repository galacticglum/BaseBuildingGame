using UnityEngine;

public class CameraController
{
    private float zoomTarget;

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
            float target = Mathf.Lerp(Camera.main.orthographicSize, zoomTarget, Settings.getSettingAsFloat("ZoomLerp", 3) * Time.deltaTime);
            Camera.main.orthographicSize = Mathf.Clamp(target, 3f, 25f);
        }

        Vector3 newMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        newMousePosition.z = 0;

        Vector3 distance = oldMousePosition - newMousePosition;
        Camera.main.transform.Translate(distance);
    }

    public void Zoom(float amount)
    {
        zoomTarget = Camera.main.orthographicSize - Settings.getSettingAsFloat("ZoomSensitivity", 3) * (Camera.main.orthographicSize * amount);
    }
}
