public delegate void CharacterCreatedEventHandler(object sender, CharacterCreatedEventArgs args);
public class CharacterCreatedEventArgs : CharacterEventArgs
{
    public CharacterCreatedEventArgs(Character character) : base(character)
    {
    }
}
