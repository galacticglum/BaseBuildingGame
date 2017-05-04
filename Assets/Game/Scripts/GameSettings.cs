using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

public static class GameSettings 
{
    private static readonly Dictionary<string, string> settings = new Dictionary<string, string>();
    private static readonly string settingsFilePath = Path.Combine("Settings", "Settings.xml");

    static GameSettings()
    {
        Load();
    }

    public static string Get(string key, string defaultValue)
    {
        string setting = Get(key);
        if (setting != null) return setting;

        settings.Add(key, defaultValue);
        Save();
        return defaultValue;
    }

    public static int Get(string key, int defaultValue)
    {
        string setting = Get(key, defaultValue.ToString());
        int value;

        if (int.TryParse(setting, out value)) return value;

        Debug.LogWarning("GameSettings::GetAsInt: Could not parse setting " + key + " of value " + setting + " to type int");
        return defaultValue;
    }

    public static float Get(string key, float defaultValue)
    {
        string setting = Get(key, defaultValue.ToString());
        float value;

        if (float.TryParse(setting, out value)) return value;

        Debug.LogWarning("GameSettings::GetAsInt: Could not parse setting " + key + " of value " + setting + " to type float");
        return defaultValue;
    }

    public static bool Get(string key, bool defaultValue)
    {
        string setting = Get(key, defaultValue.ToString());
        bool value;

        if (bool.TryParse(setting, out value)) return value;

        Debug.LogWarning("GameSettings::GetAsInt: Could not parse setting " + key + " of value " + setting + " to type bool");
        return defaultValue;
    }

    private static string Get(string key)
    {
        string value;
        if (settings.TryGetValue(key, out value)) return value;

        Debug.LogWarning("GameSettings::Get: Attempted to access a setting that was not loaded from Settings.xml :\t" + key);
        return null;
    }

    public static void Set(string key, object obj)
    {
        Set(key, obj.ToString());
    }

    public static void Set(string key, string value)
    {
        if (settings.ContainsKey(key))
        {
            settings.Remove(key); 
            settings.Add(key, value);
        }
        else 
        {
            settings.Add(key, value);
        }

        Save();
    }

    private static void Load()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, settingsFilePath);
        string furnitureXmlText = File.ReadAllText(filePath);

        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(furnitureXmlText);

        XmlNode settingsNode = xmlDocument.GetElementsByTagName("Settings").Item(0);
        if (settingsNode == null) return;

        XmlNodeList childNodes = settingsNode.ChildNodes;
        foreach (XmlNode node in childNodes)
        {
            if (node != null)
            {
                settings.Add(node.Name, node.InnerText);
            }
        }
    }

    private static void Save()
    {
        XmlDocument xmlDocument = new XmlDocument();
        XmlNode node = xmlDocument.CreateElement("Settings");

        foreach (KeyValuePair<string, string> pair in settings)
        {
            XmlElement settingElement = xmlDocument.CreateElement(pair.Key);
            settingElement.InnerText = pair.Value;

            node.AppendChild(settingElement);
        }

        xmlDocument.AppendChild(node);

        string filePath = Path.Combine(Application.streamingAssetsPath, settingsFilePath);
        xmlDocument.Save(filePath);
    }
}
