using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using Random = UnityEngine.Random;

public static class CharacterNameManager
{
    private static Dictionary<CharacterGender, List<string>> firstNames;
    private static List<string> lastNames;
    private static List<string> usedNames;

    static CharacterNameManager()
    {
        usedNames = new List<string>();
    }

    public static string Get(CharacterGender gender = CharacterGender.Any)
    {
        int count = 0;
        while (count <= 20)
        {
            if (count == 20)
            {
                usedNames = new List<string>();
            }

            if (gender == CharacterGender.Any)
            {
                gender = CharacterGender.Male;
                if (Random.value > 0.5)
                {
                    gender = CharacterGender.Female;
                }
            }

            if (!firstNames.ContainsKey(gender)) return string.Empty;
            string firstName = firstNames[gender][Random.Range(0, firstNames[gender].Count)] + " ";
            string middleName = string.Empty;
            if (Random.value < 0.25)
            {
                middleName = firstNames[gender][Random.Range(0, firstNames[gender].Count)] + " ";
            }

            string lastName = lastNames[Random.Range(0, lastNames.Count)];
            string name = firstName + middleName + lastName;
            if (!usedNames.Contains(name))
            {
                usedNames.Add(name);
                return name;
            }
            count++;
        }

        return null;
    }

    private static void Add(string firstName, CharacterGender gender)
    {
        if (firstNames.ContainsKey(gender))
        {
            firstNames[gender].Add(firstName);
        }
        else
        {
            firstNames.Add(gender, new List<string>());
            firstNames[gender].Add(firstName);
        }
    }

    private static void Add(string lastName)
    {
        lastNames.Add(lastName);
    }

    private static void RegisterName(string name, CharacterGender gender)
    {
        switch (gender)
        {
            case CharacterGender.Male:
                Add(name, CharacterGender.Male);
                break;

            case CharacterGender.Female:
                Add(name, CharacterGender.Female);
                break;

            case CharacterGender.Any:
                Add(name, CharacterGender.Male);
                Add(name, CharacterGender.Female);
                break;
        }
    }

    public static void Load(string xmlSourceText)
    {
        firstNames = new Dictionary<CharacterGender, List<string>>();
        lastNames = new List<string>();

        XmlReader reader = new XmlTextReader(new StringReader(xmlSourceText));
        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "FirstNames":
                    if (reader.ReadToDescendant("Name"))
                    {
                        do
                        {
                            string genderAttribute = reader.GetAttribute("Gender");
                            CharacterGender gender = string.IsNullOrEmpty(genderAttribute) ? CharacterGender.Any :
                                                     (CharacterGender)Enum.Parse(typeof(CharacterGender), genderAttribute);
                            reader.Read();
                            RegisterName(reader.ReadContentAsString(), gender);

                        }
                        while (reader.ReadToNextSibling("Name"));
                    }
                    else
                    {
                        Debug.LogWarning("CharacterManager::Load: Could not find any elements of name 'Name' in the Xml defintion file.");
                    }
                    break;

                case "LastNames":
                    if (reader.ReadToDescendant("Name"))
                    {
                        do
                        {
                            reader.Read();
                            Add(reader.ReadContentAsString());
                        }
                        while (reader.ReadToNextSibling("Name"));
                    }
                    else
                    {
                        Debug.LogWarning("CharacterManager::Load: Could not find any elements of name 'Name' in the Xml defintion file.");
                    }
                    break;
            }
        }
    }
}