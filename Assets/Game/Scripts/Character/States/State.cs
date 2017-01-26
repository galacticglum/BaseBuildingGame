public abstract class State
{
    public string Name { get; protected set; }
    public State NextState { get; protected set; }

    protected Character Character { get; set; }

    protected State(string name, Character character, State nextState)
    {
        Name = name;
        Character = character;
        NextState = nextState;
    }

    public abstract void Update(float deltaTime);

    public virtual void Enter() { }
    public virtual void Exit() { }

    public void Cancel()
    {
        if (NextState == null) return;

        NextState.Cancel();
        NextState = null;
    }

    protected void End()
    {
        // TODO: Update character state
    }

    public override string ToString()
    {
        return string.Format("State: {0}", Name);
    }
}