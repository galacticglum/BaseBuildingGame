using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityUtilities.ObjectPool;

public class FileLoadDialogBox : FileDialogBox
{
    public void Load()
    {
        string fileName = GetComponentInChildren<InputField>().text;
        string filePath = Path.Combine(SaveDirectoryBasePath, Path.ChangeExtension(fileName, ".save"));

        if (string.IsNullOrEmpty(fileName))
        {
            Debug.Log("FileLoadDialogBox::DoLoad: Filename is empty, no can do!");
            return;
        }

        Close();
        WorldController.Instance.Load(filePath);
    }
}
