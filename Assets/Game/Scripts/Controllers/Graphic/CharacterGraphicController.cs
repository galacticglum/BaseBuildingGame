﻿using UnityEngine;
using System.Collections.Generic;

public class CharacterGraphicController : MonoBehaviour
{
    private static World world
    {
        get { return WorldController.Instance.World; }
    }

    private Dictionary<Character, GameObject> characterGameObjectMap;
    private Dictionary<string, Sprite> characterSprites;

    // Use this for initialization
    private void Start()
    {
        LoadSprites();
        characterGameObjectMap = new Dictionary<Character, GameObject>();
        world.CharacterCreated += OnCharacterCreated;

        foreach (Character character in world.Characters)
        {
            OnCharacterCreated(this, new CharacterCreatedEventArgs(character));
        }
    }

    private void LoadSprites()
    {
        characterSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Characters/");

        foreach (Sprite s in sprites)
        {
            characterSprites[s.name] = s;
        }
    }

    public void OnCharacterCreated(object sender, CharacterCreatedEventArgs args)
    {
        GameObject characterGameObject = new GameObject();
        characterGameObjectMap.Add(args.Character, characterGameObject);
        characterGameObject.name = "Character";
        characterGameObject.transform.position = new Vector2(args.Character.X, args.Character.Y);
        characterGameObject.transform.SetParent(transform, true);

        SpriteRenderer spriteRenderer = characterGameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = characterSprites["p1_front"];
        spriteRenderer.sortingLayerName = "Characters";

        args.Character.CharacterChanged += OnCharacterChanged;
    }

    private void OnCharacterChanged(object sender, CharacterChangedEventArgs args)
    {
        if (characterGameObjectMap.ContainsKey(args.Character) == false)
        {
            Debug.LogError("CharacterGraphicController::OnCharacterChanged: Trying to change visuals for character not in our map.");
            return;
        }

        GameObject characterGameObject = characterGameObjectMap[args.Character];
        characterGameObject.transform.position = new Vector2(args.Character.X, args.Character.Y);
    }
}