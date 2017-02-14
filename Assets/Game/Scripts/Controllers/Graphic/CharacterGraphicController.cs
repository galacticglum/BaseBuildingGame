using System.Collections.Generic;
using UnityEngine;

public class CharacterGraphicController
{
    private readonly Dictionary<Character, GameObject> characterGameObjectMap;

    private readonly GameObject parent;
    private readonly Color[] swapSpriteColors;

    public CharacterGraphicController()
    {
        parent = new GameObject("Characters");
        characterGameObjectMap = new Dictionary<Character, GameObject>();

        Texture2D colorSwapTex = new Texture2D(256, 1, TextureFormat.RGBA32, false, false)
        {
            filterMode = FilterMode.Point
        };

        for (int i = 0; i < colorSwapTex.width; ++i)
        {
            colorSwapTex.SetPixel(i, 0, new Color(0.0f, 0.0f, 0.0f, 0.0f));
        }

        colorSwapTex.Apply();
        swapSpriteColors = new Color[colorSwapTex.width];

        World.Current.cbCharacterCreated += OnCharacterCreated;
        foreach (Character character in World.Current.Characters)
        {
            OnCharacterCreated(character);
        }
    }

    public void OnCharacterCreated(Character character)
    {
        GameObject characterGameObject = new GameObject();

        characterGameObjectMap.Add(character, characterGameObject);
        characterGameObject.name = "Character";
        characterGameObject.transform.position = new Vector3(character.X, character.Y, 0);
        characterGameObject.transform.SetParent(parent.transform, true);

        SpriteRenderer spriteRenderer = characterGameObject.AddComponent<SpriteRenderer>();        
        spriteRenderer.sortingLayerName = "Characters";
        
        spriteRenderer.material = GetMaterial(character);
        character.Animator = new CharacterAnimator(character, spriteRenderer)
        {
            Frames = new[]
            {
                SpriteManager.Current.GetSprite("Character", "tp2_idle_south"),
                SpriteManager.Current.GetSprite("Character", "tp2_idle_east"),
                SpriteManager.Current.GetSprite("Character", "tp2_idle_north"),
                SpriteManager.Current.GetSprite("Character", "tp2_walk_east_01"),
                SpriteManager.Current.GetSprite("Character", "tp2_walk_east_02"),
                SpriteManager.Current.GetSprite("Character", "tp2_walk_north_01"),
                SpriteManager.Current.GetSprite("Character", "tp2_walk_north_02"),
                SpriteManager.Current.GetSprite("Character", "tp2_walk_south_01"),
                SpriteManager.Current.GetSprite("Character", "tp2_walk_south_02")
            }
        };

        GameObject inventoryGameObject = new GameObject("Inventory");
        SpriteRenderer inventorySpriteRenderer = inventoryGameObject.AddComponent<SpriteRenderer>();
        inventorySpriteRenderer.sortingOrder = 1;
        inventorySpriteRenderer.sortingLayerName = "Characters";
        inventoryGameObject.transform.SetParent(characterGameObject.transform);
        inventoryGameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f); 
        inventoryGameObject.transform.localPosition = new Vector3(0, -0.37f, 0); 

        character.cbCharacterChanged += OnCharacterChanged;        
    }

    private void OnCharacterChanged(Character character)
    {
        if (characterGameObjectMap.ContainsKey(character) == false)
        {
            Debug.LogError("CharacterGraphicController::OnCharacterChanged: Trying to change visuals for character not in our map.");
            return;
        }

        SpriteRenderer inventorySpriteRenderer = characterGameObjectMap[character].transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>();
        inventorySpriteRenderer.sprite = character.Inventory != null ? SpriteManager.Current.GetSprite("Inventory", character.Inventory.GetName()) : null;

        GameObject characterGameObject = characterGameObjectMap[character];
        characterGameObject.transform.position = new Vector3(character.X, character.Y, 0);
    }

    private Material GetMaterial(Character character)
    {
        Texture2D colourSwapTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false, false)
        {
            filterMode = FilterMode.Point
        };

        for (int i = 0; i < colourSwapTexture.width; ++i)
        {
            colourSwapTexture.SetPixel(i, 0, new Color(0.0f, 0.0f, 0.0f, 0.0f));
        }

        colourSwapTexture.Apply();
  
        Color newColorLight = Color.Lerp(character.GetCharacterColor(), ColorFromRgb(255, 255, 255), 0.5f);
        Color newColorDark = Color.Lerp(character.GetCharacterColor(), ColorFromRgb(0, 0, 0), 0.5f);

        // TODO: Do something similar for hair colour, when we have characters with visible hair
        colourSwapTexture = SwapColor(colourSwapTexture, CharacterSwapSpriteColour.UniformColour, character.GetCharacterColor());
        colourSwapTexture = SwapColor(colourSwapTexture, CharacterSwapSpriteColour.UniformColourLight, newColorLight);
        colourSwapTexture = SwapColor(colourSwapTexture, CharacterSwapSpriteColour.UniformColourDark, newColorDark);
        colourSwapTexture.Apply();

        Material swapMaterial = new Material(Resources.Load<Material>("Shaders/ColorSwap"));
        Shader swapShader = Resources.Load<Shader>("Shaders/Sprites-ColorSwap");
        swapMaterial.shader = swapShader;
        swapMaterial.SetTexture("_SwapTex", colourSwapTexture);

        return swapMaterial;
    }

    private Texture2D SwapColor(Texture2D tex, CharacterSwapSpriteColour index, Color color)
    {
        swapSpriteColors[(int)index] = color;
        tex.SetPixel((int)index, 0, color);
        return tex;
    }

    public static Color ColorFromRgb(int red, int green, int blue)
    {
        return new Color(red / 255.0f, green / 255.0f, blue / 255.0f, 1.0f);
    }
}
