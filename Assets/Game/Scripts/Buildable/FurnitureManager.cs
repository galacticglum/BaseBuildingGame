using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

public class FurnitureManager : IEnumerable<Furniture>, IXmlSerializable
{
    public List<Furniture> Furnitures { get; private set; }

}