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

        LoadLua();
        LoadEvents();
    }

    private void Update()
    {
        foreach(WorldEvent worldEvent in worldEvents.Values)
        {
            worldEvent.Update(Time.deltaTime);
        }
    }

    private void LoadEvents()
    {
        worldEvents = new Dictionary<string, WorldEvent>();
        LoadEventsFromDirectory(Path.Combine(Application.streamingAssetsPath, "GameEvents"));
    }

    private static void LoadLua()
    {
        string lua = File.ReadAllText(Path.Combine(Path.Combine(Application.streamingAssetsPath, "LUA"), "GameEvent.lua"));
        WorldEventActions worldEventActions = new WorldEventActions(lua);
    }

    private void LoadEventsFromDirectory(string filePath)
    {
        string[] subDirs = Directory.GetDirectories(filePath);
        foreach (string sd in subDirs)
        {
            LoadEventsFromDirectory(sd);
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

        string xmlText = System.IO.File.ReadAllText(filePath);
        XmlTextReader reader = new XmlTextReader(new StringReader(xmlText));

        if (reader.ReadToDescendant("Events") && reader.ReadToDescendant("Event"))
        {
            do
            {
                ReadEventFromXml(reader);
            } while(reader.ReadToNextSibling("Event"));
        }
        else
        {
            Debug.LogError("Could not read the event file: " + filePath);
        }
    }

    private void ReadEventFromXml(XmlReader reader)
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
            CreateEvent(worldEventName, repeat, maxRepeats, preconditionNames.ToArray(), onExecuteNames.ToArray());
        }
    }

    private void CreateEvent(string eventName, bool repeat, int maxRepeats, string[] preconditionNames, string[] onExecuteNames)
    {
        WorldEvent worldEvent = new WorldEvent(eventName, repeat, maxRepeats);
        worldEvent.RegisterPreconditions(preconditionNames);
        worldEvent.RegisterExecutionActions(onExecuteNames);
        worldEvents[eventName] = worldEvent;
    }
}
