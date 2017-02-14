using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class OverlayDescriptor
{
    public string Id { get; private set; }
    public ColourMap ColourMap { get; private set; }
    public string LuaFunctionName { get; private set; }

    private int min;
    private int max = 255;

    private static OverlayDescriptor ReadFromXml(XmlReader xmlReader)
    {
        xmlReader.Read();
        Debug.Assert(xmlReader.Name == "Overlay");

        OverlayDescriptor overlayDescriptor = new OverlayDescriptor
        {
            Id = xmlReader.GetAttribute("id")
        };

        if(xmlReader.GetAttribute("min") != null) overlayDescriptor.min = XmlConvert.ToInt32(xmlReader.GetAttribute("min"));
        if (xmlReader.GetAttribute("max") != null) overlayDescriptor.max = XmlConvert.ToInt32(xmlReader.GetAttribute("max"));
        if (xmlReader.GetAttribute("color_map") != null)
        {
            try
            {
                overlayDescriptor.ColourMap = (ColourMap) Enum.Parse(typeof(ColourMap), xmlReader.GetAttribute("color_map"));
            }
            catch (ArgumentException e)
            {
                Debug.LogError(string.Format("Invalid color map! {0}", e));
            }
        }
        xmlReader.Read();
        overlayDescriptor.LuaFunctionName = xmlReader.ReadContentAsString();
        return overlayDescriptor;
    }

    public static Dictionary<string, OverlayDescriptor> ReadPrototypes(string fileName)
    {
        string XmlFile = System.IO.Path.Combine(Application.streamingAssetsPath, System.IO.Path.Combine("Overlay", fileName));
        XmlReader xmlReader = XmlReader.Create(XmlFile);
        Dictionary<string, OverlayDescriptor> descriptors = new Dictionary<string, OverlayDescriptor>();

        while (xmlReader.ReadToFollowing("Overlay"))
        {
            if (!xmlReader.IsStartElement() || xmlReader.GetAttribute("id") == null) continue;

            XmlReader overlayReader = xmlReader.ReadSubtree();
            descriptors[xmlReader.GetAttribute("id")] = ReadFromXml(overlayReader);
            overlayReader.Close();
        }

        return descriptors;
    }
}
