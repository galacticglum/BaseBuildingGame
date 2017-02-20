using UnityEngine;
using UnityEngine.UI;

public class DialogBoxTradeItem : MonoBehaviour
{
    public Text ItemNameText;
    public Text PlayerStockText;
    public Text PlayerSellItemPriceText;
    public Text TraderStockText;
    public Text TraderSellItemPriceText;
    public InputField TradeAmountText;

    public event TradeChangedEventHandler TradeAmountChangedEvent;
    public void OnTradeAmountChanged()
    {
        GenerateInterface();
        TradeChangedEventHandler tradeChanged = TradeAmountChangedEvent;
        if (tradeChanged != null)
        {
            tradeChanged(this, new TradeEventArgs(item));
        }
    }

    private TradeItem item;

    public void SetupTradeItem(TradeItem tradeItem)
    {
        item = tradeItem;
        GenerateInterface();
    }

    private void GenerateInterface()
    {
        ItemNameText.text = item.Type;
        PlayerStockText.text = item.PlayerStock.ToString();
        PlayerSellItemPriceText.text = item.PlayerSellItemPrice.ToString();
        TraderStockText.text = item.TraderStock.ToString();
        TraderSellItemPriceText.text = item.TraderSellItemPrice.ToString();
        TradeAmountText.text = item.TradeAmount.ToString();
    }

    public void PlayerBuyOneMore()
    {
        item.TradeAmount++;
        OnTradeAmountChanged();
    }

    public void TraderBuyOneMore()
    {
        item.TradeAmount--;
        OnTradeAmountChanged();
    }

    public void PlayerBuyAll()
    {
        item.TradeAmount = item.TraderStock;
        OnTradeAmountChanged();
    }

    public void TraderBuyAll()
    {
        item.TradeAmount = -item.PlayerStock;
        OnTradeAmountChanged();
    }
}