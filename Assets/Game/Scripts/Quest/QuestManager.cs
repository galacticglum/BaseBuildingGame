using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class QuestManager : IEnumerable<Quest>
{
    private readonly List<Quest> quests;

    public QuestManager()
    {
        quests = new List<Quest>();
        Initialize();
    }

    private void Initialize()
    {
        string filePath = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Quest.xml");
        Load(File.ReadAllText(filePath));

        DirectoryInfo[] mods = WorldController.Instance.ModManager.ModDirectories;
        foreach (DirectoryInfo mod in mods)
        {
            string traderXmlModFile = Path.Combine(mod.FullName, "Quest.xml");
            if (!File.Exists(traderXmlModFile)) continue;

            string questXmlModText = File.ReadAllText(traderXmlModFile);
            Load(questXmlModText);
        }
    }

    private void Load(string questXmlText)
    {
        XmlTextReader reader = new XmlTextReader(new StringReader(questXmlText));
        if (reader.ReadToDescendant("Quests"))
        {
            if (reader.ReadToDescendant("Quest"))
            {
                do
                {
                    Quest quest = new Quest();
                    try
                    {
                        quest.ReadXmlPrototype(reader);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error reading quest for: " + quest.Name + Environment.NewLine + "Exception: " + e.Message + Environment.NewLine + "StackTrace: " + e.StackTrace);
                    }

                    quests.Add(quest);
                }
                while (reader.ReadToNextSibling("Quest"));
            }
            else
            {
                Debug.LogError("The quest prototype definition file doesn't have any 'Quest' elements.");
            }
        }
        else
        {
            Debug.LogError("Did not find a 'Quests' element in the prototype definition file.");
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return quests.GetEnumerator();
    }

    public IEnumerator<Quest> GetEnumerator()
    {
        return ((IEnumerable<Quest>) quests).GetEnumerator();
    }
}
