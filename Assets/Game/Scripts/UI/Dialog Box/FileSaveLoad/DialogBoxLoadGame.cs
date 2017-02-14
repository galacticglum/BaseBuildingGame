using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class DialogBoxLoadGame : DialogBoxLoadSaveGame
{
    public Component FileComponent { get; set; }

    [SerializeField]
    private GameObject dialogBoxObject;
    [SerializeField]
    private bool hasPressedDelete;
    public bool HasPressedDelete
    {
        get { return hasPressedDelete; }
        set { hasPressedDelete = value; }
    }

    public override void Show()
    {
        base.Show();

        DialogListItem[] listItems = GetComponentsInChildren<DialogListItem>();
        foreach (DialogListItem listItem in listItems)
        {
            listItem.doubleclick = OnClick;
        }
    }

    private void Update()
    {
        if (hasPressedDelete)
        {
            SetButtonLocation(FileComponent);
        }
    }

    public void SetButtonLocation(Component item)
    {
        GameObject buttonObject = GameObject.FindGameObjectWithTag("DeleteButton");
        buttonObject.transform.position = new Vector3(item.transform.position.x + 110f, item.transform.position.y - 8f);
    }

    public override void OnClick()
    {
        string fileName = gameObject.GetComponentInChildren<InputField>().text;
        string saveDirectoryPath = WorldController.Instance.FileSaveBasePath;

        ValidateDirectory(saveDirectoryPath);

        string filePath = Path.Combine(saveDirectoryPath, fileName + ".sav");
        if (File.Exists(filePath) == false)
        {
            Close();
            return;
        }

        Close();
        LoadWorld(filePath);
    }

    public override void Close()
    {
        GameObject go = GameObject.FindGameObjectWithTag("DeleteButton");
        go.GetComponent<Image>().color = new Color(255, 255, 255, 0);
        hasPressedDelete=false;
        base.Close();
    }

    public void DeleteFile()
    {
        string fileName = gameObject.GetComponentInChildren<InputField>().text;
        string saveDirectoryPath = WorldController.Instance.FileSaveBasePath;

        ValidateDirectory(saveDirectoryPath);

        string filePath = Path.Combine(saveDirectoryPath, fileName + ".sav");
        if (File.Exists(filePath) == false)
        {
            Close();
            return;
        }

        CloseSureDialog();
        File.Delete(filePath);

        Close();
        Show();
    }

    public void CloseSureDialog()
    {
        dialogBoxObject.SetActive(false);
    }

    public void DeleteWasClicked()
    {
        dialogBoxObject.SetActive(true);
    }

    public void LoadWorld(string filePath)
    {
        WorldController.Instance.LoadWorld(filePath);
    }
}
