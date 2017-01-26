public static class PrototypeManager
{
    public static PrototypeContainer<Furniture> Furnitures { get; private set; }

    static PrototypeManager()
    {
        Furnitures = new PrototypeContainer<Furniture>("Furnitures", "Furniture");
    }
}
