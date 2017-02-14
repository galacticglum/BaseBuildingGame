using System;
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

    public event Action<TradeItem> OnTradeAmountChangedEvent;

    private TradeItem item;

    public void SetupTradeItem(TradeItem item)
    {
        this.item = item;
        BindInterface();
    }

    private void BindInterface()
    {
        ItemNameText.text = item.Type;
        PlayerStockText.text = item.PlayerStock.ToString();
        PlayerSellItemPriceText.text = item.PlayerSellItemPrice.ToString();
        TraderStockText.text = item.TraderStock.ToString();
        TraderSellItemPriceText.text = item.TraderSellItemPrice.ToString();
        TradeAmountText.text = item.TradeAmount.ToString();
    }

    public void OnTradeAmountChanged()
    {
        BindInterface();
        if (OnTradeAmountChangedEvent != null)
        {
            OnTradeAmountChangedEvent(this.item);
        }
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