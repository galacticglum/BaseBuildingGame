using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Trade
{
    public Trader Player { get; private set; }
    public Trader Trader { get; private set; }
    public List<TradeItem> Items { get; set; }

    public float CurrencyBalance
    {
        get { return Items.Sum(i => i.CurrencyBalance); }
    }

    public Trade(Trader player, Trader trader)
    {
        Player = player;
        Trader = trader;

        List<Inventory> totalStock = new List<Inventory>();
        totalStock.AddRange(player.Stock);
        totalStock.AddRange(trader.Stock);
        Items = totalStock.GroupBy(s => s.Type).Select(g => new TradeItem
        {
            Type = g.Key,
            BasePrice = g.First().BasePrice,
            PlayerStock = player.Stock.Where(s => s.Type == g.Key).Sum(s => s.StackSize),
            TraderStock = trader.Stock.Where(s => s.Type == g.Key).Sum(s => s.StackSize),
            TradeAmount = 0,
            PlayerSellItemPrice = g.First().BasePrice*player.SaleMarginMultiplier,
            TraderSellItemPrice = g.First().BasePrice*trader.SaleMarginMultiplier
        }).ToList();
    }

    public void Accept()
    {
        // TODO
        Debug.Log(string.Format("{0} accepted a trade with {1}.", Player.Name, Trader.Name));
    }

    public bool IsValid()
    {
        // TODO
        return true; 
    }
}