using System;

public class ContextMenuEventArgs : EventArgs
{
    public readonly ContextMenuAction ContextMenuAction;
    public readonly Character Character;

    public ContextMenuEventArgs(ContextMenuAction contextMenuAction, Character character)
    {
        ContextMenuAction = contextMenuAction;
        Character = character;
    }
}