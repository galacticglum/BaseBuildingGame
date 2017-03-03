using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldGenerator
{
    public static TileType AsteroidTileType { get; set; }

    private static int startAreaWidth;
    private static int startAreaHeight;
    private static int startAreaCenterX;
    private static int startAreaCenterY;
    
    private static int[,] startAreaTiles = new int[0, 0];
    private static string[,] startAreaFurnitures = new string[0, 0];
    
    private static float asteroidNoiseScale = 0.2f;
    private static float asteroidNoiseThreshhold = 0.75f;
    private static float asteroidResourceChance = 0.15f;

    private static Inventory[] resources;
    private static int[] resourceMin;
    private static int[] resourceMax;

    public static void Generate(World world, int seed)
    {
        Random.InitState(seed);

        AsteroidTileType = TileType.Parse("Floor");
        ReadXml();

        int width = world.Width;
        int height = world.Height;

        int xOffset = Random.Range(0, 10000);
        int yOffset = Random.Range(0, 10000);

        int totalWeightedChance = resources.Sum(resource => resource.StackSize);
        for (int x = 0; x < startAreaWidth; x++)
        {
            for (int y = 0; y < startAreaHeight; y++)
            {
                int worldX = width / 2 - startAreaCenterX + x;
                int worldY = height / 2 + startAreaCenterY - y;

                world.GetTileAt(worldX, worldY).Type = TileType.GetTileTypes()[startAreaTiles[x, y]];
            }
        }

        for (int x = 0; x < startAreaWidth; x++)
        {
            for (int y = 0; y < startAreaHeight; y++)
            {
                int worldX = width / 2 - startAreaCenterX + x;
                int worldY = height / 2 + startAreaCenterY - y;

                Tile tile = world.GetTileAt(worldX, worldY);
                if (startAreaFurnitures[x, y] != null && startAreaFurnitures[x, y] != string.Empty)
                {
                    world.FurnitureManager.Place(startAreaFurnitures[x, y], tile, true);
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noiseValue = Mathf.PerlinNoise((x + xOffset) / (width * asteroidNoiseScale), (y + yOffset) / (height * asteroidNoiseScale));
                if (!(noiseValue >= asteroidNoiseThreshhold) || IsStartArea(x, y, world)) continue;

                Tile tileAt = world.GetTileAt(x, y);
                tileAt.Type = AsteroidTileType;

                if (!(Random.value <= asteroidResourceChance) || tileAt.Furniture != null) continue;
                if (resources.Length <= 0) continue;

                int currentWeight = 0;
                int weight = Random.Range(0, totalWeightedChance);

                for (int i = 0; i < resources.Length; i++)
                {
                    Inventory inventory = resources[i];

                    int inventoryWeight = inventory.StackSize; // In stacksize the weight was cached
                    currentWeight += inventoryWeight;

                    if (weight > currentWeight) continue;

                    int stackSize = Random.Range(resourceMin[i], resourceMax[i]);
                    if (stackSize > inventory.MaxStackSize)
                    {
                        stackSize = inventory.MaxStackSize;
                    }

                    world.InventoryManager.Place(tileAt, new Inventory(inventory.Type, inventory.MaxStackSize, stackSize));
                    break;
                }
            }
        }
    }
    
    public static bool IsStartArea(int x, int y, World world)
    {
        int boundX = world.Width / 2 - startAreaCenterX;
        int boundY = world.Height / 2 + startAreaCenterY;

        return x >= boundX && x < (boundX + startAreaWidth) && y >= (boundY - startAreaHeight) && y < boundY;
    }

    public static void ReadXml()
    {
        // Setup XML Reader
        string filePath = Path.Combine(Application.streamingAssetsPath, "Data");
        filePath = Path.Combine(filePath, "WorldGenerator.xml");
        string furnitureXmlText = File.ReadAllText(filePath);

        XmlTextReader reader = new XmlTextReader(new StringReader(furnitureXmlText));
        if (reader.ReadToDescendant("WorldGenerator"))
        {
            if (reader.ReadToDescendant("Asteroid"))
            {
                try
                {
                    XmlReader asteroid = reader.ReadSubtree();
                    while (asteroid.Read())
                    {
                        switch (asteroid.Name)
                        {
                            case "NoiseScale":
                                reader.Read();
                                asteroidNoiseScale = asteroid.ReadContentAsFloat();
                                break;
                            case "NoiseThreshhold":
                                reader.Read();
                                asteroidNoiseThreshhold = asteroid.ReadContentAsFloat();
                                break;
                            case "ResourceChance":
                                reader.Read();
                                asteroidResourceChance = asteroid.ReadContentAsFloat();
                                break;
                            case "Resources":
                                XmlReader subReader = reader.ReadSubtree();

                                List<Inventory> resource = new List<Inventory>();
                                List<int> minResource = new List<int>();
                                List<int> maxResource = new List<int>();

                                while (subReader.Read())
                                {
                                    if (subReader.Name != "Resource") continue;
                                    resource.Add(new Inventory(subReader.GetAttribute("objectType"), int.Parse(subReader.GetAttribute("maxStack")), 
                                        Mathf.CeilToInt(float.Parse(subReader.GetAttribute("weightedChance")))));

                                    minResource.Add(int.Parse(subReader.GetAttribute("min")));
                                    maxResource.Add(int.Parse(subReader.GetAttribute("max")));
                                }

                                resources = resource.ToArray();
                                resourceMin = minResource.ToArray();
                                resourceMax = maxResource.ToArray();

                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error reading WorldGenerator/Asteroid" + Environment.NewLine + "Exception: " + e.Message + Environment.NewLine + "StackTrace: " + e.StackTrace);
                }
            }
            else
            {
                Debug.LogError("Did not find a 'Asteroid' element in the WorldGenerator definition file.");
            }

            if (reader.ReadToNextSibling("StartArea"))
            {
                try
                {
                    startAreaWidth = int.Parse(reader.GetAttribute("width"));
                    startAreaHeight = int.Parse(reader.GetAttribute("height"));
                    startAreaCenterX = int.Parse(reader.GetAttribute("centerX"));
                    startAreaCenterY = int.Parse(reader.GetAttribute("centerY"));
                    startAreaTiles = new int[startAreaWidth, startAreaHeight];

                    XmlReader subReader = reader.ReadSubtree();
                    while (subReader.Read())
                    {
                        switch (subReader.Name)
                        {
                            case "Tiles":
                                reader.Read();
                                string tilesString = subReader.ReadContentAsString();
                                string[] splittedString = tilesString.Split(","[0]);

                                if (splittedString.Length < startAreaWidth * startAreaHeight)
                                {
                                    Debug.LogError("Error reading 'Tiles' array to short: " + splittedString.Length + " !");
                                    break;
                                }

                                for (int x = 0; x < startAreaWidth; x++)
                                {
                                    for (int y = 0; y < startAreaHeight; y++)
                                    {
                                        startAreaTiles[x, y] = int.Parse(splittedString[x + (y * startAreaWidth)]);
                                    }
                                }

                                break; 
                            case "Furnitures":
                                XmlReader furnitureReader = reader.ReadSubtree();
                                startAreaFurnitures = new string[startAreaWidth, startAreaHeight];

                                while (furnitureReader.Read())
                                {
                                    if (furnitureReader.Name != "Furniture") continue;

                                    int x = int.Parse(furnitureReader.GetAttribute("x"));
                                    int y = int.Parse(furnitureReader.GetAttribute("y"));
                                    startAreaFurnitures[x, y] = furnitureReader.GetAttribute("name");
                                }

                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error reading WorldGenerator/StartArea" + System.Environment.NewLine + "Exception: " + e.Message + System.Environment.NewLine + "StackTrace: " + e.StackTrace);
                }
            }
            else
            {
                Debug.LogError("Did not find a 'StartArea' element in the WorldGenerator definition file.");
            }
        }
        else
        {
            Debug.LogError("Did not find a 'WorldGenerator' element in the WorldGenerator definition file.");
        }
    }
}