using System;

public class CharacterEventArgs : EventArgs
{
    public readonly Character Character;

    public CharacterEventArgs(Character character) : base()
    {
        Character = character;
    }
} 
