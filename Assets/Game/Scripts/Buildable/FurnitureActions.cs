using MoonSharp.Interpreter;
using UnityEngine;

public static class FurnitureActions
{
    public static void CallFunctionsWithFurniture<T>(string[] functionNames, T target, float deltaTime)
    {
        if (target == null)
        {
            Debug.LogError("Furniture is null, cannot call LUA function (something is fishy).");
        }

        foreach (string functionName in functionNames)
        {
            if (string.IsNullOrEmpty(functionName))
            {
                return;
            }
           
            DynValue result = LuaUtilities.CallFunction(functionName, target, deltaTime);
            if (result.Type == DataType.String)
            {
                Debug.Log(result.String);
            }
        }
    }
    
    public static void BuildFurniture(object sender, JobEventArgs args)
    {
        WorldController.Instance.World.PlaceFurniture(args.Job.Type, args.Job.Tile);
        args.Job.Tile.PendingBuildJob = null;
    }
}
