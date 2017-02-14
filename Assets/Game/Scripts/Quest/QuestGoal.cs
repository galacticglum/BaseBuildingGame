using System.Xml;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class QuestGoal
{
    public string Description { get; private set; }
    public bool IsCompleted { get; set; }

    public ParameterContainer Parameters { get; private set; }
    public string IsCompletedLuaFunction { get; set; }

    public void ReadXmlPrototype(XmlReader parentReader)
    {
        Description = parentReader.GetAttribute("Description");
        IsCompletedLuaFunction = parentReader.GetAttribute("IsCompletedLuaFunction");

        XmlReader reader = parentReader.ReadSubtree();
        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Params":
                    Parameters = ParameterContainer.ReadXml(reader);
                    break;
            }
        }
    }
}