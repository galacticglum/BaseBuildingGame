using UnityEngine;
using MoonSharp.Interpreter;
using System;
using System.IO;

public static class LuaUtilities
{

    private static readonly Script luaScript;

    static LuaUtilities()
    {
        // Tell the LUA interpreter system to load all the classes
        // that we have marked as [MoonSharpUserData]
        luaScript = new Script();

        UserData.RegisterAssembly();
        ExposeType<Inventory>(luaScript);
        ExposeType<Job>(luaScript);
        ExposeType(typeof(ModUtils));
        ExposeType<World>(luaScript);

        ExposeType<FurnitureEventArgs>(luaScript);

        ExposeType<JobPriority>(luaScript);
    }

    public static DynValue CallFunction(string functionName, params object[] args)
    {
        object func = luaScript.Globals[functionName];

        if (func == null)
        {
            Debug.LogError("'" + functionName + "' is not a LUA function!");
        }

        try
        {
            return luaScript.Call(func, args);
        }
        catch (InterpreterException e)
        {
            Debug.LogError(e.DecoratedMessage);
            return null;
        }
    }

    public static void LoadScriptFromFile(string filePath)
    {
        string luaCode = System.IO.File.ReadAllText(filePath);

        try
        {
            luaScript.DoString(luaCode);
        }
        catch (InterpreterException e)
        {
            Debug.LogError("[" + Path.GetFileName(filePath) + "] LUA Parse error: " + e.DecoratedMessage);
        }
    }

    private static void ExposeType(Type type)
    {
        luaScript.Globals[type.Name] = type;
    }

    private static void ExposeType<T>(Script lua)
    {
        if (!UserData.IsTypeRegistered<T>())
        {
            UserData.RegisterType<T>();
        }

        lua.Globals[typeof(T).Name] = UserData.CreateStatic<T>();
    }
}
