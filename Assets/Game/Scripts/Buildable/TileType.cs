using UnityEngine;
using System.Collections.Generic;
using System;
using MoonSharp.Interpreter;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Linq;

[MoonSharpUserData]
public class TileType : IXmlSerializable
{
    public static TileType Empty { get { return Parse("Empty"); } }
    public static TileType Floor { get { return Parse("Floor"); } }

    private static readonly Dictionary<string, TileType> tileTypes = new Dictionary<string, TileType>();
    private static readonly Dictionary<TileType, Job> tileTypesJobPrototypes = new Dictionary<TileType, Job>();

    public string Type { get; protected set; }
    public string Name { get; protected set; }
    public string Description { get; protected set; }

    public float BaseMovementCost { get; private set; }
    public bool LinksToNeighbours { get; private set; }

    public Job JobPrototype { get { return tileTypesJobPrototypes.ContainsKey(this) ? tileTypesJobPrototypes[this].Clone() : null; } }

    public string MovementCostLua { get; private set; }
    public string CanBuildHereLua { get; private set; }
    public string LocalizationCode { get; private set; }
    public string UnlocalizedDescription { get; private set; }

    private TileType()
    {
        MovementCostLua = "MovementCost_Standard";
        CanBuildHereLua = "CanBuildHere_Standard";
    }

    private TileType(string name, string description, float baseMovementCost)
    {
        Name = name;
        Description = description;
        BaseMovementCost = baseMovementCost;

        tileTypes[name] = this;
    }

    public static TileType Parse(string type)
    {
        return tileTypes.ContainsKey(type) ? tileTypes[type] : null;
    }

    public static TileType[] GetTileTypes()
    {
        return tileTypes.Values.ToArray();
    }

    public override string ToString()
    {
        return Type;
    }

    public static void Load()
    {
        LuaUtilities.LoadScriptFromFile(Path.Combine(Path.Combine(Application.streamingAssetsPath, "LUA"), "Tile.lua"));
        foreach (DirectoryInfo mod in WorldController.Instance.ModManager.ModDirectories)
        {
            foreach (FileInfo file in mod.GetFiles("Tiles.lua"))
            {
                Debug.Log("Loading mod " + mod.Name + " TileType definitions!");

                LuaUtilities.LoadScriptFromFile(file.FullName);
            }
        }

        LoadTileTypesXml(File.ReadAllText(Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Tiles.xml")));

        foreach (DirectoryInfo mod in WorldController.Instance.ModManager.ModDirectories)
        {
            foreach (FileInfo file in mod.GetFiles("Tiles.xml"))
            {
                LoadTileTypesXml(File.ReadAllText(file.FullName));
            }
        }
    }

    private static void LoadTileTypesXml(string xml)
    {
        XmlTextReader reader = new XmlTextReader(new StringReader(xml));
        if (reader.ReadToDescendant("Tiles"))
        {
            if (reader.ReadToDescendant("Tile"))
            {
                do
                {
                    TileType type = new TileType();
                    type.ReadXml(reader);

                    tileTypes[type.Type] = type;

                } while (reader.ReadToNextSibling("Tile"));
            }
            else
            {
                Debug.LogError("TileType::LoadTileTypesXml: Could not find a 'Tile' element in the Xml definition file.");
            }
        }
        else
        {
            Debug.LogError("TileType::LoadTileTypesXml: Could not find a 'Tiles' element in the Xml definition file.");
        }
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader parentReader)
    {
        Type = parentReader.GetAttribute("tileType");
        XmlReader reader = parentReader.ReadSubtree();

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Name":
                    reader.Read();
                    Name = reader.ReadContentAsString();
                    break;
                case "Description":
                    reader.Read();
                    Description = reader.ReadContentAsString();
                    break;
                case "BaseMovementCost":
                    reader.Read();
                    BaseMovementCost = reader.ReadContentAsFloat();
                    break;
                case "LinksToNeighbours":
                    reader.Read();
                    LinksToNeighbours = reader.ReadContentAsBoolean();
                    break;
                case "BuildingJob":
                    float jobTime = float.Parse(reader.GetAttribute("jobTime"));
                    JobPriority priority = JobPriority.High;
                    bool repeatingJob = false;
                    bool workAdjacent = true;

                    List<Inventory> inventories = new List<Inventory>();
                    XmlReader readerSubtree = reader.ReadSubtree();

                    while (readerSubtree.Read())
                    {
                        switch (readerSubtree.Name)
                        {
                            case "JobPriority":
                                readerSubtree.Read();
                                priority = (JobPriority)Enum.Parse(typeof(JobPriority), reader.ReadContentAsString());
                                break;
                            case "RepeatingJob":
                                readerSubtree.Read();
                                repeatingJob = reader.ReadContentAsBoolean();
                                break;
                            case "WorkAdjacent":
                                readerSubtree.Read();
                                workAdjacent = reader.ReadContentAsBoolean();
                                break;
                            case "Inventory":
                                inventories.Add(new Inventory(readerSubtree.GetAttribute("objectType"), int.Parse(readerSubtree.GetAttribute("amount")), 0));
                                break;
                        }
                    }

                    Job job = new Job(null, this, Tile.OnJobComplete, jobTime, inventories.ToArray(), priority, repeatingJob)
                    {
                        Description = "job_build_" + Type + "_desc",
                        WorkAdjacent = workAdjacent
                    };

                    tileTypesJobPrototypes[this] = job;

                    break;
                case "MovementCost":
                    string movementCostAttribute = reader.GetAttribute("FunctionName");
                    if (movementCostAttribute != null)
                    {
                        MovementCostLua = movementCostAttribute;
                    }

                    break;
                case "CanPlaceHere":
                    string canPlaceHereAttribute = reader.GetAttribute("FunctionName");
                    if (canPlaceHereAttribute != null)
                    {
                        CanBuildHereLua = canPlaceHereAttribute;
                    }

                    break;
                case "LocalizationCode":
                    reader.Read();
                    LocalizationCode = reader.ReadContentAsString();
                    break;
                case "UnlocalizedDescription":
                    reader.Read();
                    UnlocalizedDescription = reader.ReadContentAsString();
                    break;
            }
        }
    }

    public void WriteXml(XmlWriter writer)
    {
        throw new NotSupportedException();
    }
}
