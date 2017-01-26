using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class CharacterManager : IEnumerable<Character>, IXmlSerializable
{
    private readonly List<Character> characters;

    public LuaEventManager EventManager { get; protected set; }
    public event CharacterCreatedEventHandler CharacterCreated;
    public void OnCharacterCreated(CharacterEventArgs args)
    {
        CharacterCreatedEventHandler characterCreated = CharacterCreated;
        if (characterCreated != null)
        {
            characterCreated(this, args);
        }

        EventManager.Trigger("CharacterCreated", this, args);
    }

    public CharacterManager()
    {
        characters = new List<Character>();
        EventManager = new LuaEventManager("CharacterCreated");
    }

    public Character Create(Tile tile)
    {
        Character characterInstance = new Character(tile);
        characters.Add(characterInstance);

        OnCharacterCreated(new CharacterEventArgs(characterInstance));
        return characterInstance;
    }

    public void Update(float deltaTime)
    {
        foreach (Character character in characters)
        {
            character.Update(deltaTime);
        }
    }

    public IEnumerable<Character> Find(Func<Character, bool> filter)
    {
        return characters.Where(filter);
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

            Character character = Create(World.Current.GetTileAt(x, y));
            character.ReadXml(reader);
        }
        while (reader.ReadToNextSibling("Character"));
    }
}
