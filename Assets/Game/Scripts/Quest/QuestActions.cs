using MoonSharp.Interpreter;

public class QuestActions
{
    private static QuestActions Instance;
    private readonly Script lua;

    public QuestActions(string rawLuaCode)
    {
        Instance = this;

        UserData.RegisterAssembly();
        lua = new Script();

        lua.Globals["Inventory"] = typeof(Inventory);
        lua.Globals["Quest"] = typeof(Quest);
        lua.Globals["QuestGoal"] = typeof(QuestGoal);
        lua.Globals["QuestReward"] = typeof(QuestReward);
        lua.Globals["ModUtils"] = typeof(ModUtils);
        lua.Globals["World"] = typeof(World);

        lua.DoString(rawLuaCode);
    }

    public static DynValue CallFunction(string functionName, params object[] args)
    {
        object func = Instance.lua.Globals[functionName];

        return Instance.lua.Call(func, args);
    }
}