using UnityEngine.UI;
using System.Xml.Serialization;
using System.IO;

public class DialogBoxSaveGame : DialogBoxLoadSaveGame
{
    public override void Show()
    {
        base.Show();
        DialogListItem[] listItems = GetComponentsInChildren<DialogListItem>();
        foreach (DialogListItem listItem in listItems)
        {
            listItem.doubleclick = OnClick;
        }
    }

    public override void OnClick()
    {
        string fileName = gameObject.GetComponentInChildren<InputField>().text;
        string filePath = Path.Combine(WorldController.Instance.FileSaveBasePath, fileName + ".sav");

        Close();
        SaveWorld(filePath);
    }

    public void SaveWorld(string filePath)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextWriter writer = new StringWriter();
        serializer.Serialize(writer, WorldController.Instance.World);
        writer.Close();

        if (Directory.Exists(WorldController.Instance.FileSaveBasePath) == false)
        {
            Directory.CreateDirectory(WorldController.Instance.FileSaveBasePath);
        }

        File.WriteAllText(filePath, writer.ToString());
    }
}
