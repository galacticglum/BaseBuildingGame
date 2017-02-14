using MoonSharp.Interpreter;
using UnityEngine;

public class NeedActions
{
    private static NeedActions Instance;
    private readonly Script lua;

    public NeedActions()
    {
        Instance = this;

        UserData.RegisterAssembly();
        lua = new Script();

        lua.Globals["Inventory"] = typeof(Inventory);
        lua.Globals["Job"] = typeof(Job);
        lua.Globals["World"] = typeof(World);
    }

    public static void AddScript(string rawLuaCode)
    {
        Instance.lua.DoString(rawLuaCode);
    }
    
    public static void CallFunctionsWithNeed(string[] functionNames, Need need, float deltaTime)
    {
        foreach (string fn in functionNames)
        {
            object func = Instance.lua.Globals[fn];

            if (func == null)
            {
                Debug.LogError("'" + fn + "' is not a LUA function.");
                return;
            }

            DynValue result = Instance.lua.Call(func, need, deltaTime);

            if (result.Type == DataType.String)
            {
                Debug.Log(result.String);
            }
        }
    }

    public static DynValue CallFunction(string functionName, params object[] args)
    {
        object func = Instance.lua.Globals[functionName];

        return Instance.lua.Call(func, args);
    }
}
