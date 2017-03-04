using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter; 
using UnityEngine;

[MoonSharpUserData]
public class CharacterManager : IEnumerable<Character>, IXmlSerializable
{
    private readonly List<Character> characters;

    public event CharacterCreatedEventHandler CharacterCreated;
    public void OnCharacterCreated(CharacterEventArgs args)
    {
        CharacterCreatedEventHandler characterCreated = CharacterCreated;
        if (characterCreated != null)
        {
            characterCreated(this, args);
        }
    }

    public CharacterManager()
    {
        characters = new List<Character>();
    }

    public Character Create(Tile tile)
    {
        Character character = new Character(tile);
        InitializeCharacter(character);
        return character;
    }

    public Character Create(Tile tile, Color colour)
    {
        Character character = new Character(tile, colour);
        InitializeCharacter(character);
        return character;
    }

    public void Update(float deltaTime)
    {
        foreach (Character character in characters)
        {
            character.Update(deltaTime);
        }
    }

    private void InitializeCharacter(Character character)
    {
        // TODO: Make character names Xml
        string filePath = Path.Combine(Application.streamingAssetsPath, "Data");
        filePath = Path.Combine(filePath, "CharacterNames.txt");

        string[] names = File.ReadAllLines(filePath);
        character.Name = names[UnityEngine.Random.Range(0, names.Length - 1)];
        characters.Add(character);

        OnCharacterCreated(new CharacterEventArgs(character));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return characters.GetEnumerator();
    }

    public IEnumerator<Character> GetEnumerator()
    {
        return ((IEnumerable<Character>) characters).GetEnumerator();
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("Characters");
        foreach (Character character in characters)
        {
            writer.WriteStartElement("Character");
            character.WriteXml(writer);
            writer.WriteEndElement();

        }
        writer.WriteEndElement();
    }

    public void ReadXml(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Character")) return;
        do
        {
            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            if (reader.GetAttribute("r") != null)
            {
                float r = float.Parse(reader.GetAttribute("r"));
                float b = float.Parse(reader.GetAttribute("b")); ;
                float g = float.Parse(reader.GetAttribute("g")); ;
                Color colour = new Color(r, g, b, 1.0f);
                Character character = Create(World.Current.GetTileAt(x, y), colour);
                character.ReadXml(reader);
            }

            else
            {
                Character character = Create(World.Current.GetTileAt(x, y));
                character.ReadXml(reader);
            }

        }
        while (reader.ReadToNextSibling("Character"));
    }
}
