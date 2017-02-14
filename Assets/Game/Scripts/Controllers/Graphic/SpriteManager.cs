using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

// The sprite manager is responsible for loading sprites and keeping
// them in memory; it does not handle gameobject creation.
// Possibly make it a static class?
public class SpriteManager : MonoBehaviour
{
    public static SpriteManager Current { get; private set; }
    public static Texture2D MissingTexture { get; private set; }

    private Dictionary<string, Sprite> sprites;

    private void Awake()
    {
        if (MissingTexture != null) return;

        MissingTexture = new Texture2D(32, 32, TextureFormat.ARGB32, false);
        Color32[] pixels = MissingTexture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(255, 0, 255, 255);
        }

        MissingTexture.SetPixels32(pixels);
        MissingTexture.Apply();
    }

    private void OnEnable()
    {
        Current = this;
        LoadSprites();
    }

    private void LoadSprites()
    {
        sprites = new Dictionary<string, Sprite>();
        string filePath = Path.Combine(Application.streamingAssetsPath, "Images");

        LoadSprites(filePath);

        DirectoryInfo[] mods = WorldController.Instance.ModManager.ModDirectories;
        foreach (DirectoryInfo mod in mods)
        {
            string modImagesPath = Path.Combine(mod.FullName, "Images");
            if (Directory.Exists(modImagesPath))
            {
                LoadSprites(modImagesPath);
            }
        }
    }

    private void LoadSprites(string filePath)
    {
        string[] directories = Directory.GetDirectories(filePath);
        foreach (string directory in directories)
        {
            LoadSprites(directory);
        }

        string[] files = Directory.GetFiles(filePath);
        foreach (string file in files)
        {
            string spriteCategory = new DirectoryInfo(filePath).Name;
            LoadImage(spriteCategory, file);
        }
    }

    private void LoadImage(string spriteCategory, string filePath)
    {
        if (filePath.Contains(".xml") || filePath.Contains(".meta") || filePath.Contains(".db"))
        {
            return;
        }

        // Load the file into a texture
        byte[] imageBytes = File.ReadAllBytes(filePath);
        Texture2D imageTexture = new Texture2D(2, 2);

        if (!imageTexture.LoadImage(imageBytes)) return;

        string baseSpriteName = Path.GetFileNameWithoutExtension(filePath);
        string xmlPath = Path.Combine(Path.GetDirectoryName(filePath), baseSpriteName + ".xml");

        if (File.Exists(xmlPath))
        {
            XmlTextReader reader = new XmlTextReader(new StringReader(File.ReadAllText(xmlPath)));
            if (reader.ReadToDescendant("Sprites") && reader.ReadToDescendant("Sprite"))
            {
                do
                {
                    ReadSpriteFromXml(spriteCategory, reader, imageTexture);
                }
                while (reader.ReadToNextSibling("Sprite"));
            }
            else
            {
                Debug.LogError("Could not find a <Sprites> tag.");
            }

        }
        else
        {
            LoadSprite(spriteCategory, baseSpriteName, imageTexture, new Rect(0, 0, imageTexture.width, imageTexture.height), 32, new Vector2(0.5f, 0.5f));
        }
    }

    private void ReadSpriteFromXml(string spriteCategory, XmlReader reader, Texture2D imageTexture)
    {
        string spriteName = reader.GetAttribute("name");
        int x = int.Parse(reader.GetAttribute("x"));
        int y = int.Parse(reader.GetAttribute("y"));
        int width = int.Parse(reader.GetAttribute("w"));
        int height = int.Parse(reader.GetAttribute("h"));

        string pivotXAttribute = reader.GetAttribute("pivotX");
        float pivotX;

        if (float.TryParse(pivotXAttribute, out pivotX) == false)
        {
            pivotX = 0.5f;
        }


        string pivotYAttribute = reader.GetAttribute("pivotY");
        float pivotY;

        if (float.TryParse(pivotYAttribute, out pivotY) == false)
        {
            pivotY = 0.5f;
        }

        pivotX = Mathf.Clamp01(pivotX);
        pivotY = Mathf.Clamp01(pivotY);
        
        LoadSprite(spriteCategory, spriteName, imageTexture, new Rect(x, y, width, height), int.Parse(reader.GetAttribute("pixelPerUnit")), new Vector2(pivotX, pivotY));
    }

    private void LoadSprite(string spriteCategory, string spriteName, Texture2D imageTexture, Rect spriteCoordinates, int pixelsPerUnit, Vector2 pivotPoint)
    {
        spriteName = spriteCategory + "/" + spriteName;

        Sprite sprite = Sprite.Create(imageTexture, spriteCoordinates, pivotPoint, pixelsPerUnit);
        sprite.texture.filterMode = FilterMode.Point;
        sprites[spriteName] = sprite;
    }

    public Sprite GetSprite(string categoryName, string spriteName)
    {
        spriteName = categoryName + "/" + spriteName;
        return sprites.ContainsKey(spriteName) == false ? Sprite.Create(MissingTexture, new Rect(Vector2.zero, new Vector3(32, 32)), new Vector2(0.5f, 0.5f), 32) : sprites[spriteName];
    }
}
