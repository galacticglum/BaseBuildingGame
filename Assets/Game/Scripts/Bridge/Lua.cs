using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MoonSharp.Interpreter;

public static class Lua
{
    private static readonly List<string> parsedFilePaths;
    private static readonly Script lua;
     
    static Lua()
    {
        lua = new Script();
        LuaAPI.Register(lua);

        parsedFilePaths = new List<string>();
    }

    public static void Parse(string relativePath)
    {
        string fullFilePath = Path.Combine(Application.streamingAssetsPath, relativePath);

        if (parsedFilePaths.Contains(fullFilePath)) return;
        parsedFilePaths.Add(fullFilePath);

        try
        {
            lua.DoString(File.ReadAllText(fullFilePath));
        }
        catch (InterpreterException e)
        {
            LogException(e);
        }
        catch (Exception e)
        {
            LogException(e);
        }
    }

    public static DynValue Call(string function, params object[] args)
    {
        DynValue result = null;
        try
        {
            result = lua.Call(lua.Globals[function], args);
        }
        catch (InterpreterException e)
        {
            LogException(e);
        }
        catch (Exception e)
        {
            LogException(e);
        }

        return result;
    }

    public static DynValue Call(Closure function, params object[] args)
    {
        DynValue result = null;
        try
        {
            result = function.Call(args);
        }
        catch (InterpreterException e)
        {
            LogException(e);
        }
        catch (Exception e)
        {
            LogException(e);
        }

        return result;
    }

    public static Closure GetFunction(string functionName)
    {
        Closure function = (Closure) lua.Globals[functionName];
        if (function == null)
        {
            Debug.LogWarning("Lua::GetFunction: Tried to get a non-existent function of name '" + functionName + "'.");
        }

        return function;
    }

    private static void LogException(InterpreterException e)
    {
        string decoratedMessage = e.DecoratedMessage;
        string culpritFilePath = parsedFilePaths[int.Parse(decoratedMessage.Substring(6, decoratedMessage.IndexOf(":", StringComparison.Ordinal) - 6)) - 1];
        Debug.LogError(decoratedMessage + "\n" + "at " + culpritFilePath.Substring(culpritFilePath.IndexOf("StreamingAssets", StringComparison.Ordinal)).Replace('\\', Path.AltDirectorySeparatorChar));
    }

    private static void LogException(Exception e)
    {
        Debug.LogWarning(e.Message);
    }
}

