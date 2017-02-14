using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DialogBoxTrade : DialogBox
{
    public Text TraderNameText;
    public Text PlayerCurrencyBalanceText;
    public Text TraderCurrencyBalanceText;
    public Text TradeCurrencyBalanceText;
    public Transform TradeItemListPanel;
    public GameObject TradeItemPrefab;

    private Trade trade;

    public void SetupTrade(Trade trade)
    {
        this.trade = trade;

        ClearInterface();
        BuildInterface();
    }

    private void ClearInterface()
    {
        List<Transform> childrens = TradeItemListPanel.Cast<Transform>().ToList();
        foreach (Transform child in childrens)
        {
            Destroy(child.gameObject);
        }
    }

    public void DoTradingTestWithMockTraders()
    {
        Trader mockPlayer = new Trader
        {
            CurrencyBalance = 500,
            Name = "Player",
            SaleMarginMultiplier = 1f,
            Stock = new List<Inventory>
            {
                new Inventory("Steel Plate",50,10){ BasePrice = 3f},
                new Inventory("Raw Iron",100,90){ BasePrice = 0.2f},
            }
        };

        Trader mockTrader = new Trader
        {
            CurrencyBalance = 1500,
            Name = "Trader",
            SaleMarginMultiplier = 1.23f,
            Stock = new List<Inventory>
            {
                new Inventory("Steel Plate",50,40){ BasePrice = 3f},
                new Inventory("Steel Plate",50,40){ BasePrice = 3f},
                new Inventory("Oxygen Bottle",10,10){ BasePrice = 50f},
            }
        };

        SetupTrade(new Trade(mockPlayer, mockTrader));
    }

    private void BuildInterface()
    {
        TraderNameText.text = trade.Trader.Name;
        GenerateHeader();

        foreach (TradeItem tradeItem in trade.Items)
        {
            GameObject instance = Instantiate(TradeItemPrefab);
            instance.transform.SetParent(TradeItemListPanel);

            DialogBoxTradeItem tradeItemBehaviour = instance.GetComponent<DialogBoxTradeItem>();
            tradeItemBehaviour.OnTradeAmountChangedEvent += item => GenerateHeader();
            tradeItemBehaviour.SetupTradeItem(tradeItem);
        }
    }

    private void GenerateHeader()
    {
        float tradeAmount = trade.CurrencyBalance;
        PlayerCurrencyBalanceText.text = (trade.Player.CurrencyBalance + tradeAmount).ToString();
        TraderCurrencyBalanceText.text = (trade.Trader.CurrencyBalance - tradeAmount).ToString();
        TradeCurrencyBalanceText.text = tradeAmount.ToString();
    }

    public void CancelTrade()
    {
        trade = null;
        ClearInterface();
        Close();
    }

    public void AcceptTrade()
    {
        if (!trade.IsValid()) return;

        trade.Accept();
        trade = null;
        ClearInterface();
        Close();
    }
}