using System.Xml;

public class InventoryPrototype : IPrototypable
{
    public string Type { get; private set; }
    public int MaxStackSize { get; private set; }

    public void ReadXmlPrototype(XmlReader reader)
    {
        Type = reader.GetAttribute("Type");
        XmlReader subReader = reader.ReadSubtree();
        while (subReader.Read())
        {
            switch (subReader.Name)
            {
                case "MaxStackSize":
                    subReader.Read();
                    MaxStackSize = subReader.ReadContentAsInt();
                    break;
            }
        }
    }
}