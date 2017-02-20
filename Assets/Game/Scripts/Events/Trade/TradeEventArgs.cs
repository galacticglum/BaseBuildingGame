using System;

public class TradeEventArgs : EventArgs
{
    public readonly TradeItem TradeItem;
    public TradeEventArgs(TradeItem tradeItem)
    {
        TradeItem = tradeItem;
    }
}