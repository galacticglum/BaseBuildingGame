using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class QuestManager : IEnumerable<Quest>
{
    private readonly List<Quest> quests;
    private float totalDeltaTime;
    private readonly float checkDelayInSeconds;

    public QuestManager()
    {
        quests = new List<Quest>();
        checkDelayInSeconds = 5f;
    }

    public void Initialize()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "LUA");
        filePath = Path.Combine(filePath, "Quest.lua");
        Lua.Parse(filePath);

        filePath = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Quest.xml");
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

    public void Update(float deltaTime)
    {
        totalDeltaTime += deltaTime;
        if (!(totalDeltaTime >= checkDelayInSeconds)) return;

        totalDeltaTime = 0f;
        CheckAllAcceptedQuests();
    }

    private void CheckAllAcceptedQuests()
    {
        List<Quest> ongoingQuests = World.Current.QuestManager.Where(q => q.IsAccepted && !q.IsCompleted).ToList();
        foreach (Quest quest in ongoingQuests)
        {
            if (IsQuestCompleted(quest))
            {
                CollectQuestReward(quest);
            }
        }

        List<Quest> completedQuestWithUnCollectedRewards = World.Current.QuestManager.Where(q => q.IsCompleted && q.Rewards.Any(r => !r.IsCollected)).ToList();
        foreach (Quest quest in completedQuestWithUnCollectedRewards)
        {
            if (!ongoingQuests.Contains(quest))
            {
                CollectQuestReward(quest);
            }
        }
    }

    private bool IsQuestCompleted(Quest quest)
    {
        quest.IsCompleted = true;
        foreach (QuestGoal goal in quest.Goals)
        {
            quest.EventManager.Trigger("IsQuestCompleted", goal);
            quest.IsCompleted &= goal.IsCompleted;
        }

        return quest.IsCompleted;
    }

    private void CollectQuestReward(Quest quest)
    {
        foreach (QuestReward reward in quest.Rewards)
        {
            if (!reward.IsCollected)
            {
                quest.EventManager.Trigger("QuestRewarded", reward);
            }
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
