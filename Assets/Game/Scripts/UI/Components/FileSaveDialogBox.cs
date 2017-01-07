using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityUtilities.ObjectPool;

public class FileSaveDialogBox : DialogBox
{
    private static string FileSaveBasePath  { get { return Path.Combine(Application.persistentDataPath, "Saves"); }}

    [SerializeField]
    private GameObject fileListItemPrefab;
    [SerializeField]
    private GameObject fileList;

	// Use this for initialization
	private void Start ()
    {
        Close();
    }

    public override void Show()
    {
        base.Show();

        InputField inputField = GetComponentInChildren<InputField>();
        foreach (string file in Directory.GetFiles(FileSaveBasePath, "*.save"))
        {
            GameObject listItem = ObjectPool.Spawn(fileListItemPrefab, Vector3.one, Quaternion.identity);

            listItem.GetComponent<DialogBoxListItem>().InputField = inputField;
            listItem.transform.SetParent(fileList.transform);
            listItem.GetComponentInChildren<Text>().text = Path.GetFileNameWithoutExtension(file);

            fileList.GetComponent<AutomaticVerticalSize>().Recalculate();
        }
    }

    public override void Close()
    {
        base.Close();

        for (int i = fileList.transform.childCount - 1; i >= 0; i--)
        {
            ObjectPool.Destroy(fileList.transform.GetChild(i).gameObject);
        }

        GetComponentInChildren<InputField>().text = string.Empty;
    }

    public void DoSave()
    {
        string fileName = GetComponentInChildren<InputField>().text;
        string filePath = Path.Combine(FileSaveBasePath, Path.ChangeExtension(fileName, ".save"));
        string saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");

        if (string.IsNullOrEmpty(fileName))
        {
            Debug.Log("FileSaveDialogBox::DoSave: Filename is empty, no can do!");
            return;
        }

        Close();
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        if (File.Exists(filePath))
        {
            // TODO: Show overwrite dialog box.
            Debug.Log("FileSaveDialogBox::DoSave: File already exists, this would be a good time to show an overwrite dialog box.");
            return;
        }

        File.WriteAllText(filePath, WorldController.Instance.Save());
    }
}
