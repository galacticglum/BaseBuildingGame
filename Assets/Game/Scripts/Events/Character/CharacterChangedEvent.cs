public delegate void CharacterChangedEventHandler(object sender, CharacterChangedEventArgs args);
public class CharacterChangedEventArgs : CharacterEventArgs
{
    public CharacterChangedEventArgs(Character character) : base(character)
    {
    }
}
