public static class PrototypeManager
{
    public static PrototypeContainer<Furniture> Furnitures { get; private set; }
    public static PrototypeContainer<InventoryPrototype> Inventories { get; private set; }
    public static PrototypeContainer<Need> Needs { get; private set; }
    public static PrototypeContainer<TraderPrototype> Traders { get; private set; }
    
    static PrototypeManager()
    {
        Furnitures = new PrototypeContainer<Furniture>("Furnitures", "Furniture");
        Inventories = new PrototypeContainer<InventoryPrototype>("Inventories", "Inventory");
        Needs = new PrototypeContainer<Need>("Needs", "Need");
        Traders = new PrototypeContainer<TraderPrototype>("Traders", "Trader");
    }
}