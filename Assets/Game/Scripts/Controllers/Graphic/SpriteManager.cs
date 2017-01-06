using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

// The sprite manager is responsible for loading sprites and keeping
// them in memory; it does not handle gameobject creation.
public class SpriteManager : MonoBehaviour
{
    public static SpriteManager Current { get; set; }
    private Dictionary<string, Sprite> sprites;

    private void OnEnable()
    {
        Current = this;
        sprites = new Dictionary<string, Sprite>();

        string filePath = Path.Combine(Application.streamingAssetsPath, "Images");
        LoadSprites(filePath);
    }

    private void LoadSprites(string filePath)
    {
        string[] extensions = { ".jpg", ".jpeg", ".png" };
        string[] files = Directory.GetFiles(filePath, "*", SearchOption.AllDirectories).Where(file => extensions.Contains(Path.GetExtension(file))).ToArray();
        foreach (string file in files)
        {
            LoadImage(file);
        }
    }

    private void LoadImage(string filePath)
    {
        byte[] imageData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(0, 0);

        if (!texture.LoadImage(imageData)) return;

        string spriteName = Path.GetFileNameWithoutExtension(filePath);
        string spritePath = Path.GetDirectoryName(filePath);
        string spriteCategory = new DirectoryInfo(spritePath).Name;

        string xmlFilePath = Path.Combine(spritePath, spriteName + ".xml");
        if (File.Exists(xmlFilePath))
        {
            XmlReader reader = new XmlTextReader(new StringReader(File.ReadAllText(xmlFilePath)));
            if (!reader.ReadToDescendant("Sprites")) return; // There was no sprites definition found; this could be
                                                             // because of a typo or such (do we want to log this??)

            reader.ReadToDescendant("Sprite");
            do
            {
                ReadSpriteDefinition(spriteCategory, reader, texture);
            }
            while (reader.ReadToNextSibling("Sprite"));
        }
        else
        {
            // If there is no xml definition for the sprite assume sprite is single.
            LoadSprite(spriteCategory, spriteName, texture, new Rect(0, 0, texture.width, texture.height), 32);
        }
    }

    private void LoadSprite(string spriteCategory, string spriteName, Texture2D texture, Rect coordinates, int pixelsPerUnit)
    {
        Sprite sprite = Sprite.Create(texture, coordinates, new Vector2(0.5f, 0.5f), pixelsPerUnit);
        sprite.texture.filterMode = FilterMode.Point;
        sprites[spriteCategory + "/" + spriteName] = sprite;
    }

    private void ReadSpriteDefinition(string spriteCategory, XmlReader reader, Texture2D texture)
    {
        string spriteName = reader.GetAttribute("Name");
        int x = int.Parse(reader.GetAttribute("X"));
        int y = int.Parse(reader.GetAttribute("Y"));
        int width = int.Parse(reader.GetAttribute("Width"));
        int height = int.Parse(reader.GetAttribute("Height"));
        int pixelsPerUnit = int.Parse(reader.GetAttribute("PixelsPerUnit"));

        LoadSprite(spriteCategory, spriteName, texture, new Rect(x, y, width, height), pixelsPerUnit);
    }

    public Sprite GetSprite(string spriteCategory, string spriteName)
    {
        string spritePath = spriteCategory + "/" + spriteName;
        return sprites.ContainsKey(spritePath) ? sprites[spritePath] : null;

        //Debug.LogError("SpriteManager::GetSprite: No sprite with name (category/name) '" + spritePath + "'.");
    }
}
