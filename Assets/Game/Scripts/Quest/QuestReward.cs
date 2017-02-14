using System.Xml;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class QuestReward
{
    public string Description { get; private set; }
    public bool IsCollected { get; set; }
    public ParameterContainer Parameters { get; private set; }

    public string OnRewardLuaFunction { get; set; }

    public void ReadXmlPrototype(XmlReader reader_parent)
    {
        Description = reader_parent.GetAttribute("Description");
        OnRewardLuaFunction = reader_parent.GetAttribute("OnRewardLuaFunction");

        XmlReader reader = reader_parent.ReadSubtree();

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