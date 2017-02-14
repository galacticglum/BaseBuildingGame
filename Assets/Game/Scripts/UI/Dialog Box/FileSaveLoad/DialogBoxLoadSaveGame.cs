using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;

public class DialogBoxLoadSaveGame : DialogBox
{
    public static readonly Color SecondaryColour = new Color(0.9f, 0.9f, 0.9f);

    [SerializeField]
    private GameObject fileListItemPrefab;
    [SerializeField]
    private Transform fileList;

    public void ValidateDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath)) return;
        Directory.CreateDirectory(directoryPath);
    }

    public override void Show()
    {
        base.Show();

        string saveDirectoryPath = WorldController.Instance.FileSaveBasePath;
        ValidateDirectory(saveDirectoryPath);

        DirectoryInfo saveDirectory = new DirectoryInfo(saveDirectoryPath);
        FileInfo[] saveGames = saveDirectory.GetFiles().OrderByDescending(f => f.CreationTime).ToArray();
        InputField inputField = gameObject.GetComponentInChildren<InputField>();

        for (int i = 0; i < saveGames.Length; i++)
        {
            FileInfo file = saveGames[i];
            GameObject fileObject = Instantiate(fileListItemPrefab);

            // Make sure this gameobject is a child of our list box
            fileObject.transform.SetParent(fileList);
            string fileName = Path.GetFileNameWithoutExtension(file.FullName);

            fileObject.GetComponentInChildren<Text>().text = string.Format("{0}\n<size=11><i>{1}</i></size>", fileName, file.CreationTime);

            DialogListItem listItem = fileObject.GetComponent<DialogListItem>();
            listItem.fileName = fileName;
            listItem.inputField = inputField;

            fileObject.GetComponent<Image>().color = (i % 2 == 0 ? Color.white : SecondaryColour);
        }

		fileList.GetComponentInParent<ScrollRect>().scrollSensitivity = fileList.childCount / 2.0f;
    }

    public override void Close()
    {
        while (fileList.childCount > 0)
        {
            Transform child = fileList.GetChild(0);
            child.SetParent(null);	
            Destroy(child.gameObject);
        }

        base.Close();
    }
}
