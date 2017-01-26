using System.Xml;

public interface IPrototypable
{
    string Type { get; }
    void ReadXmlPrototype(XmlReader reader);
}
