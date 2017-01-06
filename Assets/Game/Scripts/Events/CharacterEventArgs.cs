using System;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class CharacterEventArgs : EventArgs
{
    public readonly Character Character;

    public CharacterEventArgs(Character character) : base()
    {
        Character = character;
    }
} 
