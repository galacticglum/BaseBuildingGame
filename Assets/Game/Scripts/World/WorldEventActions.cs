using MoonSharp.Interpreter;
using UnityEngine;

public class WorldEventActions
{
    private static WorldEventActions Instance { get; set; }
    private Script lua;

    public WorldEventActions(string luaSource)
    {
        Instance = this;

        UserData.RegisterAssembly();
        lua = new Script();

        lua.Globals["Inventory"] = typeof(Inventory);
        lua.Globals["Job"] = typeof(Job);
        lua.Globals["ModUtils"] = typeof(ModUtils);
        lua.Globals["World"] = typeof(World);

        lua.DoString(luaSource);
    }

    public static void CallFunctionsWithEvent(string[] functionNames, WorldEvent worldEvent)
    {
        foreach (string fn in functionNames)
        {
            object func = Instance.lua.Globals[fn];

            if (func == null)
            {
                Debug.LogError("'" + fn + "' is not a LUA function.");
                return;
            }

            DynValue result = Instance.lua.Call(func, worldEvent);

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
