using UnityEngine;

public class KeyboardController
{
    private ConstructionController constructionController;
    private readonly WorldController worldController;

    // TODO: Move me to a suitable location (i.e: TimeController).
    private readonly float[] timeScale = new float[6]
    {
        0.1f, 0.5f, 1f, 2f, 4f, 8f
    };

    private int currentTimeScale = 2;

    [SerializeField, Range(0, 3)]
    private float scrollSpeed = 0.1f;

    public KeyboardController(ConstructionController constructionController, WorldController worldController)
    {
        this.constructionController = constructionController;
        this.worldController = worldController;
    }

    public void Update(bool isModal)
    {
        if (isModal)
        {
            return;
        }

        CheckCameraInput();
        CheckTimeInput();
    }

    private void CheckCameraInput()
    {
        Camera.main.transform.position += Camera.main.orthographicSize * scrollSpeed * new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);
        if (Input.GetKey(KeyCode.PageUp))
        {
            worldController.CameraController.Zoom(0.1f);
        }

        if (Input.GetKey(KeyCode.PageDown))
        {
            worldController.CameraController.Zoom(-0.1f);
        }
    }

    private void CheckTimeInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            worldController.IsPaused = !worldController.IsPaused;
        }

        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            if (currentTimeScale == timeScale.Length - 1)
            {
                return;
            }

            currentTimeScale++;
            worldController.TimeScale = timeScale[currentTimeScale];
        }
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            if (currentTimeScale == 0)
            {
                return;
            }

            currentTimeScale--;
            worldController.TimeScale = timeScale[currentTimeScale];
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            worldController.TimeScale = 1f;
            currentTimeScale = 2;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            worldController.TimeScale = 2f;
            currentTimeScale = 3;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            worldController.TimeScale = 4f;
            currentTimeScale = 4;
        }
    }
}
