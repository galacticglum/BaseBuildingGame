using System.IO;
using UnityEngine;

[AddComponentMenu("Localization/Localization Loader")]
public class LocalizationLoader : MonoBehaviour
{
    public void UpdateLocalizationTable()
    {
        Load(Application.streamingAssetsPath);
        foreach (DirectoryInfo mod in WorldController.Instance.ModManager.ModDirectories)
        {
            Load(mod.FullName);
        }

        string language = Settings.getSetting("localization", "en_US");
        LocalizationTable.CurrentLanguage = language;
        LocalizationTable.LoadingLanguagesFinished();
    }

    private void Awake()
    {
        // Check if the languages have already been loaded before.
        if (LocalizationTable.Initialized)
        {
            return;
        }

        UpdateLocalizationTable();
    }

    private static void Load(string path)
    {
        string filePath = Path.Combine(path, "Localization");

        // TODO: Think over the extension ".lang", might change that in the future.
        foreach (string file in Directory.GetFiles(filePath, "*.lang"))
        {
            LocalizationTable.Load(file);
        }
    }
}

