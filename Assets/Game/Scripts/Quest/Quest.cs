using System.Collections.Generic;
using System.Security.Policy;
using System.Xml;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Quest
{
    public string Name { get; private set; }
    public string Description { get; private set; }

    public List<string> Requirements { get; private set; }
    public List<QuestReward> Rewards { get; private set; }
    public List<QuestGoal> Goals { get; private set; }

    public LuaEventManager EventManager { get; private set; }

    public bool IsAccepted { get; set; }
    public bool IsCompleted { get; set; }

    public Quest()
    {
        EventManager = new LuaEventManager();
    }

    public void ReadXmlPrototype(XmlTextReader parentReader)
    {
        Name = parentReader.GetAttribute("Name");
        Goals = new List<QuestGoal>();
        Rewards = new List<QuestReward>();   
        Requirements = new List<string>();

        XmlReader reader = parentReader.ReadSubtree();
        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Description":
                    reader.Read();
                    Description = reader.ReadContentAsString();
                    break;
                case "PreRequiredCompletedQuests":
                    XmlReader subReader = reader.ReadSubtree();
                    while (subReader.Read())
                    {
                        if (subReader.Name == "PreRequiredCompletedQuest")
                        {
                            Requirements.Add(subReader.GetAttribute("Name"));
                        }
                    }
                    break;
                case "Goals":
                    subReader = reader.ReadSubtree();
                    while (subReader.Read())
                    {
                        if (subReader.Name != "Goal") continue;

                        QuestGoal goal = new QuestGoal(this);
                        goal.ReadXmlPrototype(subReader);
                        Goals.Add(goal);
                    }
                    break;
                case "Rewards": 
                    subReader = reader.ReadSubtree();
                    while (subReader.Read())
                    {
                        if (subReader.Name != "Reward") continue;

                        QuestReward reward = new QuestReward(this);
                        reward.ReadXmlPrototype(subReader);
                        Rewards.Add(reward);
                    }
                    break;
            }
        }
    }
}