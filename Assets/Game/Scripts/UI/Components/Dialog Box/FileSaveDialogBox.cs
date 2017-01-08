using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityUtilities.ObjectPool;

public class FileSaveDialogBox : FileDialogBox
{
    public void Save()
    {
        string fileName = GetComponentInChildren<InputField>().text;
        string filePath = Path.Combine(SaveDirectoryBasePath, Path.ChangeExtension(fileName, ".save"));
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
