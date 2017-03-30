using System.Collections.Generic;
using System.Linq;
using System.Xml;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class ParameterContainer
{
    public string Value { get; private set; }
    public bool HasContents { get { return subParameters.Count > 0; } }

    public string Name { get; private set; }
    private readonly Dictionary<string, ParameterContainer> subParameters;
    private bool isValueUninitialized = true;

    public string[] Keys
    {
        get
        {
            string[] keys = new string[subParameters.Keys.Count];
            subParameters.Keys.CopyTo(keys, 0);
            return keys;
        }
    }

    public ParameterContainer(string name, string value) 
    {
        Name = name;

        Value = value;
        subParameters = new Dictionary<string, ParameterContainer>();
        isValueUninitialized = false;
    }

    public ParameterContainer(string name, object value) 
    {
        Name = name;

        Value = value.ToString();
        subParameters = new Dictionary<string, ParameterContainer>();
        isValueUninitialized = false;
    }

    public ParameterContainer(string name) 
    {
        this.Name = name;
        subParameters = new Dictionary<string, ParameterContainer>();
    }

    public ParameterContainer(ParameterContainer parameterContainer)
    {
        Name = parameterContainer.Name;
        subParameters = parameterContainer.HasContents ? parameterContainer.Clone() : new Dictionary<string, ParameterContainer>();

        Value = parameterContainer.ToString();
    }

    private Dictionary<string, ParameterContainer> Clone()
    {
        return subParameters.ToDictionary(entry => entry.Key, entry => new ParameterContainer(entry.Value));
    }

    public ParameterContainer this[string key]
    {
        get
        {
            if (subParameters.ContainsKey(key)) return subParameters[key];

            subParameters.Add(key, new ParameterContainer(key));
            return subParameters[key];
        }
        set { subParameters[key] = value; }
    }

    public static ParameterContainer ReadXml(XmlReader reader)
    {
        XmlReader subReader = reader.ReadSubtree();
        ParameterContainer parameterGroup = new ParameterContainer(subReader.GetAttribute("name"));

        // Advance to the first inner element. Two reads are needed to ensure we don't get stuck on containing Element, or an EndElement
        subReader.Read();
        subReader.Read();

        while (subReader.ReadToNextSibling("Param"))
        {
            string name = subReader.GetAttribute("name");
            switch (subReader.NodeType)
            {
                case XmlNodeType.EndElement:
                    continue;

                case XmlNodeType.Element:
                    // An empty element is a singular Param such as <Param name="name" value="value />
                    if (subReader.IsEmptyElement)
                    {
                        string value = subReader.GetAttribute("value");
                        parameterGroup[name] = new ParameterContainer(name, value);
                    }
                    else
                    {
                        // This must be a group element, so we recurse and dive deeper
                        parameterGroup[name] = ReadXml(subReader);
                    }
                    break;
            }
        }

        subReader.Close();
        return parameterGroup;
    }

    public void AddParameter(ParameterContainer parameter)
    {
        subParameters[parameter.Name] = parameter;
    }

    public void SetValue(string value) 
    {
        Value = value;
        isValueUninitialized = false;
    }

    public void SetValue(object value)
    {
        Value = value.ToString();
        isValueUninitialized = false;
    }

    public void ModifyFloatValue(float value)
    {
        Value = string.Empty + (Float() + value);
        isValueUninitialized = false;
    }

    public float Float()
    {
        float returnValue;
        float.TryParse(Value, out returnValue);
        return returnValue;
    }

    public float Float(float defaultValue)
    {
        return isValueUninitialized ? defaultValue : Float();
    }

    public int Int()
    {
        int returnValue;
        int.TryParse(Value, out returnValue);
        return returnValue;
    }

    public bool ContainsKey(string key)
    {
        return subParameters.ContainsKey(key);
    }

    public override string ToString()
    {
        return Value;
    }

    public string ToString(string defaultValue)
    {
        return isValueUninitialized ? defaultValue : ToString();
    }

    public void WriteXml(XmlWriter writer)
    {
        if (HasContents)
        {
            WriteXmlParameterGroup(writer);
        }
        else
        {
            WriteXmlParameters(writer);
        }
    }

    public void WriteXmlParameterGroup(XmlWriter writer)
    {
        writer.WriteStartElement("Param");
        writer.WriteAttributeString("name", Name);
        if (string.IsNullOrEmpty(Value) == false)
        {
            writer.WriteAttributeString("value", Value);
        }

        foreach (string k in subParameters.Keys)
        {
            this[k].WriteXml(writer);
        }

        writer.WriteEndElement();
    }

    public void WriteXmlParameters(XmlWriter writer)
    {
        writer.WriteStartElement("Param");
        writer.WriteAttributeString("name", Name);
        writer.WriteAttributeString("value", Value);
        writer.WriteEndElement();
    }
}
