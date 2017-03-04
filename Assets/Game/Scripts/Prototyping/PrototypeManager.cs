public static class PrototypeManager
{
    public static PrototypeContainer<Furniture> Furnitures { get; private set; }
    public static PrototypeContainer<InventoryPrototype> Inventories { get; private set; }

    static PrototypeManager()
    {
        Furnitures = new PrototypeContainer<Furniture>("Furnitures", "Furniture");
        Inventories = new PrototypeContainer<InventoryPrototype>("Inventories", "Inventory");
    }
}