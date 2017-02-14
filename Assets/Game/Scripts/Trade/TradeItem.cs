using System;

public class TradeItem
{
    public string Type { get; set; }
    public float BasePrice { get; set; }

    public float PlayerSellItemPrice { get; set; }
    public float TraderSellItemPrice { get; set; }

    public int PlayerStock { get; set; }
    public int TraderStock { get; set; }

    private int tradeAmount;
    public int TradeAmount
    {
        get { return tradeAmount; }
        set
        {
            tradeAmount = value < 0 ? Math.Max(value, -PlayerStock) : Math.Min(value, TraderStock);
        }
    }

    public float CurrencyBalance
    {
        get
        {
            return TradeAmount < 0 ? -TradeAmount * PlayerSellItemPrice : -TradeAmount * TraderSellItemPrice;
        }
    }
}