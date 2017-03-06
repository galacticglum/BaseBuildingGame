using System;
using UnityEngine;
using MoonSharp.Interpreter;

public static class LuaApiHandler
{
    public static void Register(Script lua)
    {
        ExposeType<EventArgs>(lua);
        ExposeType<CharacterEventArgs>(lua);
        ExposeType<FurnitureEventArgs>(lua);
        ExposeType<JobEventArgs>(lua);
        ExposeType<TileEventArgs>(lua);

        ExposeType<LuaEventManager>(lua);

        ExposeType<World>(lua);
        ExposeType<Inventory>(lua);
        ExposeType<Job>(lua);
        ExposeType<InventoryManager>(lua);

        ExposeType<JobPriority>(lua);
        ExposeType<TileType>(lua);
        ExposeType<TileEnterability>(lua);
        
        ExposeType<Mathf>(lua);
        ExposeType<Debug>(lua);

        UserData.RegisterAssembly();
    }

    private static void ExposeType<T>(Script lua)
    {
        if (!UserData.IsTypeRegistered<T>())
        {
            UserData.RegisterType<T>();
        }

        lua.Globals[typeof(T).Name] = UserData.CreateStatic<T>();
    }

    private static void ExposeType(Type type, Script lua)
    {
        if (!UserData.IsTypeRegistered(type))
        {
            UserData.RegisterType(type);
        }

        lua.Globals[type.Name] = UserData.CreateStatic(type);
    }
}