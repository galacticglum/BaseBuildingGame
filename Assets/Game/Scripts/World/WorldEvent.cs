using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class WorldEvent
{
    public string Name { get; private set; }
    public bool CanRepeat { get; private set; }
    public int MaxRepeats { get; private set; }

    public float Timer { get; private set; }

    private readonly List<string> preconditions;
    private readonly List<string> executionActions;

    private bool executed;
    private int repeatAmount;

    public WorldEvent(string name, bool canRepeat, int maxRepeats)
    {
        Name = name;
        CanRepeat = canRepeat;
        MaxRepeats = maxRepeats;

        preconditions = new List<string>();
        executionActions = new List<string>();

        Timer = 0;
        repeatAmount = 0;
    }

    public void Update(float deltaTime)
    {
        int conditionsMet = preconditions.Sum(precondition => (int)Lua.Call(precondition, this, deltaTime).Number);
        if (conditionsMet < preconditions.Count || executed || MaxRepeats > 0 && repeatAmount >= MaxRepeats) return;

        repeatAmount++;
        Trigger();
    }

    public void Trigger()
    {
        if (executionActions != null)
        {
            Lua.Call(executionActions.ToArray(), this);
        }

        if (!CanRepeat)
        {
            executed = true;
        }
    }

    public void AddTimer(float time)
    {
        Timer += time;
    }

    public void ResetTimer()
    {
        Timer = 0;
    }

    public void RegisterPrecondition(string functionName)
    {
        preconditions.Add(functionName);
    }

    public void RegisterPreconditions(string[] functionNames)
    {
        preconditions.AddRange(functionNames);
    }

    public void RegisterExecutionAction(string functionName)
    {
        executionActions.Add(functionName);
    }

    public void RegisterExecutionActions(string[] functionNames)
    {
        executionActions.AddRange(functionNames);
    }
}
