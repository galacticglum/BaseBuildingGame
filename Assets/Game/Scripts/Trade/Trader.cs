using System.Collections.Generic;

public struct Trader
{
    public string Name { get; set; }
    public float CurrencyBalance { get; set; }
    public float SaleMarginMultiplier { get; set; }
    public List<Inventory> Stock { get; set; }
}