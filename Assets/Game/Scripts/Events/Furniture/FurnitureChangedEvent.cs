public delegate void FurnitureChangedEventHandler(object sender, FurnitureChangedEventArgs args);
public class FurnitureChangedEventArgs : FurnitureEventArgs
{
    public FurnitureChangedEventArgs(Furniture furniture) : base(furniture)
    {
    }
}
