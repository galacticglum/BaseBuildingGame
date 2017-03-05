using System.Net;
using System.Xml;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class QuestGoal
{
    public string Description { get; private set; }
    public bool IsCompleted { get; set; }

    public Quest Quest { get; private set; }
    public ParameterContainer Parameters { get; private set; }

    public QuestGoal(Quest quest)
    {
        Quest = quest;
    }

    public void ReadXmlPrototype(XmlReader parentReader)
    {
        Description = parentReader.GetAttribute("Description");
        XmlReader reader = parentReader.ReadSubtree();
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