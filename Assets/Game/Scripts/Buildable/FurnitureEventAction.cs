using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

[MoonSharpUserData]
public class FurnitureEventAction : IXmlSerializable
{
    private Dictionary<string, List<string>> actions = new Dictionary<string, List<string>>();

    public FurnitureEventAction Clone()
    {
        FurnitureEventAction furnitureEventAction = new FurnitureEventAction {actions = new Dictionary<string, List<string>>(actions)};
        return furnitureEventAction;
    }

    public void ReadXml(XmlReader reader)
    {
        reader.Read();
        if (reader.Name != "Action")
        {
            Debug.LogError(string.Format("The element is not an Action, but a \"{0}\"", reader.Name));
        }

        string name = reader.GetAttribute("event");
        if (name == null)
        {
            Debug.LogError("The attribute \"event\" is a mandatory for an \"Action\" element.");
        }

        string functionName = reader.GetAttribute("functionName");
        if (functionName == null)
        {
            Debug.LogError(string.Format("No function name was provided for the Action {0}.", name));
        }

        Register(name, functionName);
    }

    public void Register(string name, string functionName)
    {
        if (!actions.ContainsKey(name) || actions[name] == null) actions[name] = new List<string>();
        actions[name].Add(functionName);
    }

    public void Unregister(string name, string functionName)
    {
        if (!actions.ContainsKey(name) || actions[name] == null) return;
        actions[name].Remove(functionName);
    }

    public void Trigger(string name, Furniture target, float deltaTime = 0f)
    {
        if (!actions.ContainsKey(name) || actions[name] == null) return;
        FurnitureActions.CallFunctionsWithFurniture(actions[name].ToArray(), target, deltaTime);
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        foreach (string evt in actions.Keys)
        {
            writer.WriteStartElement("Action");

            foreach (string func in actions[evt])
            {
                writer.WriteAttributeString("event", evt);
                writer.WriteAttributeString("functionName", func);
            }

            writer.WriteEndElement();
        }
    }
}
