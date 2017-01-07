using System.Collections.Generic;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using UnityEngine;

public class LuaEventManager
{
    private readonly Dictionary<string, List<Closure>> luaFunctions;

    [MoonSharpVisible(false)]
    public LuaEventManager()
    {
        luaFunctions = new Dictionary<string, List<Closure>>();
    }

    [MoonSharpVisible(false)]
    public LuaEventManager(params string[] eventTags) : this()
    {
        RegisterEvents(eventTags);
    }

    [MoonSharpVisible(false)]
    public void RegisterEvent(string eventTag) 
    {
        if (luaFunctions.ContainsKey(eventTag)) return;
        luaFunctions.Add(eventTag, new List<Closure>());
    }

    [MoonSharpVisible(false)]
    public void RegisterEvents(params string[] eventTags)
    {
        foreach (string eventTag in eventTags)
        {
            RegisterEvent(eventTag);
        }
    }

    [MoonSharpVisible(false)]
    public void UnregisterEvent(string eventTag)
    {
        if (!luaFunctions.ContainsKey(eventTag)) return;
        luaFunctions.Remove(eventTag);
    }

    [MoonSharpVisible(false)]
    public void UnregisterEvents(params string[] eventTags)
    {
        foreach (string eventTag in eventTags)
        {
            UnregisterEvent(eventTag);
        }
    }

    [MoonSharpVisible(false)]
    public void Trigger(string eventTag, params object[] args)
    {
        if (!luaFunctions.ContainsKey(eventTag))
        {
            Debug.LogError("LuaEventManager::Trigger: Tried to trigger unregistered event with tag '" + eventTag + "'!");
            return;
        }

        foreach (Closure function in luaFunctions[eventTag])
        {
            Lua.Call(function, args);
        }
    }

    public void AddHandler(string eventTag, Closure function)
    {
        if (!luaFunctions.ContainsKey(eventTag)) return;
        luaFunctions[eventTag].Add(function);
    }

    public void RemoveHandler(string eventTag, Closure function)
    {
        if (!luaFunctions.ContainsKey(eventTag)) return;
        luaFunctions[eventTag].Remove(function);
    }
}

