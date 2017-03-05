using System.Collections.Generic;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;

public class LuaEventManager
{
    private Dictionary<string, List<Closure>> luaFunctions;

    public LuaEventManager Clone()
    {
        LuaEventManager eventManager = new LuaEventManager
        {
            luaFunctions = new Dictionary<string, List<Closure>>(luaFunctions)
        };

        return eventManager;
    }

    [MoonSharpVisible(false)]
    public LuaEventManager()
    {
        luaFunctions = new Dictionary<string, List<Closure>>();
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
        if (!luaFunctions.ContainsKey(eventTag) || luaFunctions[eventTag] == null) return;
        foreach (Closure function in luaFunctions[eventTag])
        {
            Lua.Call(function, args);
        }
    }

    public void AddHandler(string eventTag, Closure function)
    {
        if (!luaFunctions.ContainsKey(eventTag) || luaFunctions[eventTag] == null)
        {
            RegisterEvent(eventTag);
        }

        // ReSharper disable once PossibleNullReferenceException; RegisterEvent(string eventTag) takes care of this.
        luaFunctions[eventTag].Add(function);
    }

    public void AddHandler(string eventTag, string functionName)
    {
        if (string.IsNullOrEmpty(functionName)) return;
        if (!luaFunctions.ContainsKey(eventTag) || luaFunctions[eventTag] == null)
        {
            RegisterEvent(eventTag);
        }

        Closure function = Lua.GetFunction(functionName);
        if (function == null) return;

        // ReSharper disable once PossibleNullReferenceException; RegisterEvent(string eventTag) takes care of this.
        luaFunctions[eventTag].Add(function);
    }

    public void RemoveHandler(string eventTag, Closure function)
    {
        if (!luaFunctions.ContainsKey(eventTag) || luaFunctions[eventTag] == null) return;
        luaFunctions[eventTag].Remove(function);
    }

    public void RemoveHandler(string eventTag, string functionName)
    {
        if (!luaFunctions.ContainsKey(eventTag) || luaFunctions[eventTag] == null  || string.IsNullOrEmpty(functionName)) return;
        Closure function = Lua.GetFunction(functionName);

        if (function == null) return;
        luaFunctions[eventTag].Remove(function);
    }
}