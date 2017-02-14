using System.Xml;

public class InventoryPrototype
{
    public string Type { get; private set; }
    public int MaxStackSize { get; private set; }

    public void ReadXmlPrototype(XmlReader parentReader)
    {
        Type = parentReader.GetAttribute("objectType");
        XmlReader reader = parentReader.ReadSubtree();

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "maxStackSize":
                    reader.Read();
                    MaxStackSize = reader.ReadContentAsInt();
                    break;
            }
        }
    }
}

