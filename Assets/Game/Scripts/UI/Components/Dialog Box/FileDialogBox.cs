using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityUtilities.ObjectPool;

public class FileDialogBox : DialogBox
{
    protected static string SaveDirectoryBasePath  { get { return Path.Combine(Application.persistentDataPath, "Saves"); }}

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

        FileInfo[] files = new DirectoryInfo(SaveDirectoryBasePath).GetFiles("*.save");
        files = files.OrderByDescending(file => file.CreationTime).ToArray();

        InputField inputField = GetComponentInChildren<InputField>();
        foreach (FileInfo file in files)
        {
            GameObject listItem = ObjectPool.Spawn(fileListItemPrefab, Vector3.one, Quaternion.identity);

            listItem.GetComponent<DialogBoxListItem>().InputField = inputField;
            listItem.transform.SetParent(fileList.transform);
            listItem.GetComponentInChildren<Text>().text = Path.GetFileNameWithoutExtension(file.FullName);

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
}
