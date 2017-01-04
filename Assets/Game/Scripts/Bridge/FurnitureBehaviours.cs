using System;
using UnityEngine;
using MoonSharp.Interpreter;

public class FurnitureBehaviours
{
    public static FurnitureBehaviours Instance { get; protected set; }
    private readonly Script luaScript;

    private Callback<EventArgs> test;

    public FurnitureBehaviours(string sourceCode)
    {
        Instance = this;

        luaScript = new Script();

        UserData.RegisterType<Callback<EventArgs>>();

        UserData.RegisterType<Mathf>();
        luaScript.Globals["Mathf"] = UserData.CreateStatic<Mathf>();

        UserData.RegisterType<TileEnterability>();
        luaScript.Globals["TileEnterability"] = UserData.CreateStatic<TileEnterability>();

        UserData.RegisterType<CharacterEventArgs>();
        luaScript.Globals["CharacterEventArgs"] = UserData.CreateStatic<CharacterEventArgs>();

        UserData.RegisterType<FurnitureEventArgs>();
        luaScript.Globals["FurnitureEventArgs"] = UserData.CreateStatic<FurnitureEventArgs>();

        UserData.RegisterType<FurnitureUpdateEventArgs>();
        luaScript.Globals["FurnitureUpdateEventArgs"] = UserData.CreateStatic<FurnitureUpdateEventArgs>();

        UserData.RegisterType<JobEventArgs>();
        luaScript.Globals["JobEventArgs"] = UserData.CreateStatic<JobEventArgs>();

        UserData.RegisterType<TileEventArgs>();
        luaScript.Globals["TileEventArgs"] = UserData.CreateStatic<TileEventArgs>();

        UserData.RegisterType<World>();
        luaScript.Globals["World"] = UserData.CreateStatic<World>();

        UserData.RegisterType<Inventory>();
        luaScript.Globals["Inventory"] = UserData.CreateStatic<Inventory>();

        UserData.RegisterType<Job>();
        luaScript.Globals["Job"] = UserData.CreateStatic<Job>();

        luaScript.Options.DebugPrint = Debug.Log;
        Script.GlobalOptions.RethrowExceptionNested = true;

        UserData.RegisterAssembly();

        try
        {
            luaScript.DoString(sourceCode);
        }
        catch (ScriptRuntimeException e)
        {
            Debug.LogError("Doh! An error occured! " + e.DecoratedMessage);
            throw;
        }

    }

    public static DynValue Execute(string function, params object[] args)
    {
        try
        {
            return Instance.luaScript.Call(Instance.luaScript.Globals[function], args);

        }
        catch (ScriptRuntimeException e)
        {
            Debug.LogError("Doh! An error occured! " + e.DecoratedMessage);
            throw;
        }
    }

    public static void Execute(string[] functions, params object[] args)
    {
        foreach (string function in functions)
        {
            try
            {
                Instance.luaScript.Call(Instance.luaScript.Globals[function], args);
            }
            catch (ScriptRuntimeException e)
            {
                Debug.LogError("Doh! An error occured! " + e.DecoratedMessage);
                throw;
            }
        }
    }

    public static void BuildFurniture(object sender, JobEventArgs args)
    {
        WorldController.Instance.World.PlaceFurniture(args.Job.Type, args.Job.Tile);
        args.Job.Tile.PendingFurnitureJob = null;
    }
}
