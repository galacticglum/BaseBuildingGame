public delegate void FurnitureCreatedEventHandler(object sender, FurnitureCreatedEventArgs args);
public class FurnitureCreatedEventArgs : FurnitureEventArgs
{
    public FurnitureCreatedEventArgs(Furniture furniture) : base(furniture)
    {
    }
}