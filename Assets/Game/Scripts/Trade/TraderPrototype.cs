using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class TraderPrototype
{
    public string Type { get; private set; }
    public float MinimumCurrencyBalance { get; private set; }
    public float MaximumCurrencyBalance { get; private set; }
    public float MinimumSaleMarginMultiplier { get; private set; }
    public float MaximumSaleMarginMultiplier { get; private set; }

    public List<PotentialStock> PotentialStock { get; private set; }
    public List<string> PotentialNames { get; private set; }

    [Range(0, 1)]
    public float Rarity;

    public void ReadXmlPrototype(XmlReader parentReader)
    {
        Type = parentReader.GetAttribute("objectType");
        XmlReader reader = parentReader.ReadSubtree();

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "potentialNames":
                    PotentialNames = new List<string>();
                    XmlReader subReader = reader.ReadSubtree();
                    while (subReader.Read())
                    {
                        if (subReader.Name == "name")
                        {
                            PotentialNames.Add(subReader.Value);
                        }
                    }
                    break;
                case "minCurrencyBalance":
                    reader.Read();
                    MinimumCurrencyBalance = reader.ReadContentAsInt();
                    break;
                case "maxCurrencyBalance":
                    reader.Read();
                    MaximumCurrencyBalance = reader.ReadContentAsInt();
                    break;
                case "minSaleMarginMultiplier":
                    reader.Read();
                    MinimumSaleMarginMultiplier = reader.ReadContentAsFloat();
                    break;
                case "maxSaleMarginMultiplier":
                    reader.Read();
                    MaximumSaleMarginMultiplier = reader.ReadContentAsFloat();
                    break;
                case "potentialStock":
                    PotentialStock = new List<PotentialStock>();
                    subReader = reader.ReadSubtree();
                    while (subReader.Read())
                    {
                        if (subReader.Name == "Inventory")
                        {
                            // Found an inventory requirement, so add it to the list!
                            PotentialStock.Add(new PotentialStock
                            {
                                Type = subReader.GetAttribute("objectType"),
                                MinimumQuantity = int.Parse(subReader.GetAttribute("minQuantity")),
                                MaximumQuantity = int.Parse(subReader.GetAttribute("maxQuantity")),
                                Rarity = float.Parse(subReader.GetAttribute("rarity"))
                            });
                        }
                    }
                    break;
            }
        }
    }

    public Trader CreateTrader()
    {
        Trader trader = new Trader
        {
            Name = PotentialNames[Random.Range(PotentialNames.Count, PotentialNames.Count - 1)],
            CurrencyBalance = Random.Range(MinimumCurrencyBalance, MaximumCurrencyBalance),
            SaleMarginMultiplier = Random.Range(MinimumSaleMarginMultiplier, MaximumSaleMarginMultiplier)
        };
           
        foreach (PotentialStock stock in PotentialStock)
        {
            var itemIsInStock = Random.Range(0f, 1f) > stock.Rarity;

            if (itemIsInStock)
            {
                trader.Stock.Add(new Inventory
                {
                    Type = stock.Type,
                    StackSize = Random.Range(stock.MinimumQuantity,stock.MaximumQuantity)
                });
            }
        }

        return trader;
    }
}