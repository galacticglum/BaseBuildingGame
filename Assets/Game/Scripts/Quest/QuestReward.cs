using System.Xml;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class QuestReward
{
    public string Description { get; private set; }
    public bool IsCollected { get; set; }
    public ParameterContainer Parameters { get; private set; }

    public Quest Quest { get; private set; }

    public QuestReward(Quest quest)
    {
        Quest = quest;
    }

    public void ReadXmlPrototype(XmlReader readerParent)
    {
        Description = readerParent.GetAttribute("Description");
        XmlReader reader = readerParent.ReadSubtree();

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Params":
                    Parameters = ParameterContainer.ReadXml(reader);
                    break;
                case "Event":
                    XmlReader subtree = reader.ReadSubtree();
                    subtree.Read();

                    string eventTag = reader.GetAttribute("Tag");
                    string functionName = reader.GetAttribute("FunctionName");
                    Quest.EventManager.AddHandler(eventTag, functionName);

                    subtree.Close();
                    break;
            }
        }
    }
}