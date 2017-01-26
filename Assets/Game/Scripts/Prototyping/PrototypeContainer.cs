using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

public class PrototypeContainer<T> where T : IPrototypable, new() 
{
    public Dictionary<string, T>.KeyCollection Keys { get { return prototypeDictionary.Keys; }}
    public Dictionary<string, T>.ValueCollection Values { get { return prototypeDictionary.Values; }}
    public T this[string type] { get { return Contains(type) ? prototypeDictionary[type] : default(T); }}

    public int Count { get { return prototypeDictionary.Count; } }

    private readonly Dictionary<string, T> prototypeDictionary;
    private readonly string prototypeXmlListTag;
    private readonly string prototypeXmlElementTag;

    public PrototypeContainer()
    {
        prototypeDictionary = new Dictionary<string, T>();
    }

    public PrototypeContainer(string prototypeXmlListTag, string prototypeXmlElementTag) : this()
    {
        this.prototypeXmlListTag = prototypeXmlListTag;
        this.prototypeXmlElementTag = prototypeXmlElementTag;
    }

    public void Load(string xmlSourceText)
    {
        if (string.IsNullOrEmpty(prototypeXmlListTag) || 
            string.IsNullOrEmpty(prototypeXmlElementTag)) return;

        XmlTextReader reader = new XmlTextReader(new StringReader(xmlSourceText));
        if (reader.ReadToDescendant(prototypeXmlListTag))
        {
            if (reader.ReadToDescendant(prototypeXmlElementTag))
            {
                do
                {
                    T protoype = new T();
                    try
                    {
                        protoype.ReadXmlPrototype(reader);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("PrototypeContainer<" + prototypeXmlListTag +
                                       ">::LoadPrototypes: Error loading '" + prototypeXmlElementTag +
                                       "' prototype.\n" + e.Message);
                    }

                    Set(protoype);
                }
                while (reader.ReadToNextSibling(prototypeXmlElementTag));
            }
            else
            {
                Debug.LogError("PrototypeContainer<" + prototypeXmlListTag +
                               ">::LoadPrototypes: Could not find any elements of name '" + prototypeXmlElementTag +
                               "' in the Xml defintion file.");
            }
        }
        else
        {
            Debug.LogWarning("PrototypeContainer<" + prototypeXmlListTag +
                             ">::LoadPrototypes: Could not find element of name '" + prototypeXmlListTag +
                             "' in the Xml defintion file.");
        }
    }

    public bool Contains(string type)
    {
        return prototypeDictionary.ContainsKey(type);
    }

    public void Add(T prototype)
    {
        if (Contains(prototype.Type)) return;
        Set(prototype);
    }

    public T Get(string type)
    {
        return Contains(type) ? prototypeDictionary[type] : default(T);
    }

    public void Set(T prototype)
    {
        prototypeDictionary[prototype.Type] = prototype;
    }
}