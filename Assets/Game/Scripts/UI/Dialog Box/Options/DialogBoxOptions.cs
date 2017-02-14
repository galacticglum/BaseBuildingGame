using UnityEngine;
using UnityEngine.UI;

public class DialogBoxOptions : DialogBox
{
    [SerializeField]
    private Button buttonResume;
    [SerializeField]
    private Button buttonNewWorld;
    [SerializeField]
    private Button buttonSave;
    [SerializeField]
    private Button buttonLoad;
    [SerializeField]
    private Button buttonQuit;

    private DialogBoxController dialogController;

    private void Start()
    {
        dialogController = FindObjectOfType<DialogBoxController>();
        WorldController worldController = FindObjectOfType<WorldController>();

        buttonQuit.onClick.AddListener(OnButtonQuitGame);
        buttonResume.onClick.AddListener(Close);
        buttonNewWorld.onClick.AddListener(() => worldController.CreateWorld());

        buttonSave.onClick.AddListener(OnButtonSaveGame);
        buttonLoad.onClick.AddListener(OnButtonLoadGame);
    }

    public void OnButtonSaveGame()
    {
        Close();
        dialogController.dialogBoxSaveGame.Show();
    }

    public void OnButtonLoadGame()
    {
        Close();
        dialogController.dialogBoxLoadGame.Show();
    }

    public void OnButtonQuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
		Application.Quit();
        #endif
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Close();
        }
    }
}