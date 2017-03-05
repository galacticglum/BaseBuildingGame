using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

public class WorldEventController : MonoBehaviour
{
    public static WorldEventController Current { get; private set; }
    private Dictionary<string, WorldEvent> worldEvents;

    private void OnEnable()
    {
        Current = this;

        string filePath = Path.Combine(Path.Combine(Application.streamingAssetsPath, "LUA"), "GameEvent.lua");
        Lua.Parse(filePath);

        Load();
    }

    private void Update()
    {
        foreach(WorldEvent worldEvent in worldEvents.Values)
        {
            worldEvent.Update(Time.deltaTime);
        }
    }

    private void Load()
    {
        worldEvents = new Dictionary<string, WorldEvent>();
        LoadFromDirectory(Path.Combine(Application.streamingAssetsPath, "GameEvents"));
    }

    private void LoadFromDirectory(string filePath)
    {
        string[] subDirs = Directory.GetDirectories(filePath);
        foreach (string path in subDirs)
        {
            LoadFromDirectory(path);
        }

        string[] filesInDir = Directory.GetFiles(filePath);
        foreach (string fn in filesInDir)
        {
            LoadEvent(fn);
        }
    }

    private void LoadEvent(string filePath)
    {
        if (!filePath.Contains(".xml") || filePath.Contains(".meta"))
        {
            return;
        }

        string xmlText = File.ReadAllText(filePath);
        XmlTextReader reader = new XmlTextReader(new StringReader(xmlText));

        if (reader.ReadToDescendant("Events") && reader.ReadToDescendant("Event"))
        {
            do
            {
                ReadXml(reader);
            }
            while (reader.ReadToNextSibling("Event"));
        }
        else
        {
            Debug.LogError("Could not read the event file: " + filePath);
        }
    }

    private void ReadXml(XmlReader reader)
    {
        string worldEventName = reader.GetAttribute("Name");
        bool repeat = false;
        int maxRepeats = 0;

        List<string> preconditionNames = new List<string>();
        List<string> onExecuteNames = new List<string>();

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Repeats":
                    maxRepeats = int.Parse(reader.GetAttribute("MaxRepeats"));
                    repeat = true;
                    break;
                case "Precondition":
                    string preconditionName = reader.GetAttribute("FunctionName");
                    preconditionNames.Add(preconditionName);

                    break;
                case "OnExecute":
                    string onExecuteName = reader.GetAttribute("FunctionName");
                    onExecuteNames.Add(onExecuteName);

                    break;
            }
        }

        if (!string.IsNullOrEmpty(worldEventName))
        {
            Create(worldEventName, repeat, maxRepeats, preconditionNames.ToArray(), onExecuteNames.ToArray());
        }
    }

    private void Create(string eventName, bool repeat, int maxRepeats, string[] preconditionNames, string[] onExecuteNames)
    {
        WorldEvent worldEvent = new WorldEvent(eventName, repeat, maxRepeats);

        worldEvent.RegisterPreconditions(preconditionNames);
        worldEvent.RegisterExecutionActions(onExecuteNames);

        worldEvents[eventName] = worldEvent;
    }
}
