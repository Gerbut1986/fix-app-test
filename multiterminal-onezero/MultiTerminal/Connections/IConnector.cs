using QuickFix;
using System;
using System.Collections.Generic;

namespace MultiTerminal.Connections
{
    internal class TickEventArgs : EventArgs
    {
        public string Symbol { get; set; }
        public string SymbolId { get; set; }
        public string SubscriptionId { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
    }
    internal enum OrderSide
    {
        Buy,
        Sell
    }
    public enum OrderType
    {
        Market=0,
        Limit=1,
        Stop=2
    }
    public enum FillPolicy
    {
        FOK = 0,
        IOK = 1,
        FILL = 2
    }
    internal class OrderInformation
    {
        public string Id { get; set; }
        public OrderSide Side { get; set; }
        public string Symbol { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal Volume { get; set; }
        public decimal PnL { get; set; }
        public int Magic { get; set; }
        public int Track { get; set; }
        public DateTime OpenTime { get; set; }
        public decimal StopLoss { get; set; }
        public decimal TakeProfit { get; set; }
    }
    internal class OrderOpenResult
    {
        public string Id { get; set; }
        public decimal OpenPrice { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string Error { get; set; }
    }
    internal class OrderCloseResult
    {
        public decimal ClosePrice { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string Error { get; set; }
    }
    internal class OrderModifyResult
    {
        public TimeSpan ExecutionTime { get; set; }
        public string Error { get; set; }
    }
    internal interface IConnectorLogger
    {
        void LogInfo(string msg);
        void LogError(string msg);
        void LogWarning(string msg);
        void LogQuotes(string msg);
        void LogLogout(string msg);
    }
    internal interface IConnector
    {
        event EventHandler LoggedIn;
        event EventHandler<TickEventArgs> Tick;
        event EventHandler LoggedOut;

        void Start();
        void Stop(bool wait);
        bool IsLoggedIn { get; }
        DateTime CurrentTime { get; }
        string ViewId { get; }
        FillPolicy Fill { get; set; }

        decimal? Balance { get; }
        decimal? Equity { get; }

        void Subscribe(string symbol, string id);
        void Unsubscribe(string symbol, string id);

        // exper.
        Message CreateOrderStatusRequest();
        List<OrderInformation> GetOrders(string symbol, int magic, int track);
        OrderOpenResult OpenOrd(string symbol, decimal lot, OrderSide side, OrderType type, int price);
        OrderOpenResult Open(string symbol, decimal price, decimal lot, OrderSide side, int magic, int slippage, int track, OrderType type, int lifetimeMs);
        OrderModifyResult Modify(string symbol, string orderId, OrderSide side, decimal slPrice, decimal tpPrice);
        OrderCloseResult Close(string symbol, string orderId, decimal price, decimal volume, OrderSide side, int slippage, OrderType type, int lifetimeMs);
        bool OrderDelete(string id, string symbol, OrderType type, OrderSide side, decimal lot, decimal price);
    }

    internal class OrdersStatistic
    {
        public List<OrderInformation> Orders { get; set; }
        public decimal Profit { get; set; }
        public decimal Volume { get; set; }
        public int OrdersCount { get; set; }
        public int BuysCount { get; set; }
        public int SellsCount { get; set; }
        public OrderInformation OrderBuy { get; set; }
        public OrderInformation OrderSell { get; set; }
        public decimal BuyProfit { get; set; }
        public decimal SellProfit { get; set; }
        public OrdersStatistic(List<OrderInformation> orders, decimal bid, decimal ask)
        {
            Orders = orders;
            OrdersCount = orders.Count;
            foreach (var order in orders)
            {
                if (order.Side == OrderSide.Buy)
                {
                    BuyProfit+= bid - order.OpenPrice;
                    Profit += bid - order.OpenPrice;
                    Volume += order.Volume;
                    OrderBuy = order;
                    BuysCount++;
                }
                else
                {
                    SellProfit+= order.OpenPrice - ask;
                    Profit += order.OpenPrice - ask;
                    Volume -= order.Volume;
                    OrderSell = order;
                    SellsCount++;
                }
            }
            if (OrdersCount > 1) Profit /= orders.Count;
        }
    }

#pragma warning disable CS0067
    internal class ProxyConnector : IConnector
    {
        public bool IsLoggedIn
        {
            get
            {
                return false;
            }
        }

        public DateTime CurrentTime => DateTime.MinValue;
        public string ViewId => "Proxy";

        public FillPolicy Fill { get; set; }

        public decimal? Balance => null;
        public decimal? Equity => null;

        public event EventHandler LoggedIn;
        public event EventHandler<TickEventArgs> Tick;
        public event EventHandler LoggedOut;

        public OrderCloseResult Close(string symbol, string orderId, decimal price, decimal volume, OrderSide side,int slippage, OrderType type, int lifetimeMs)
        {
            return new OrderCloseResult() { Error = "Not implemented" };
        }

        public Message CreateOrderStatusRequest()
        {
            throw new NotImplementedException();
        }

        public List<OrderInformation> GetOrders(string symbol, int magic, int track)
        {
            return new List<OrderInformation>();
        }
        public OrderModifyResult Modify(string symbol, string orderId, OrderSide side, decimal slPrice, decimal tpPrice)
        {
            return new OrderModifyResult() { Error = "Not implemented" };
        }
        public OrderOpenResult Open(string symbol, decimal price, decimal lot, OrderSide side, int magic, int slippage, int track, OrderType type, int lifetimeMs)
        {
            return new OrderOpenResult() { Error="Not implemented" };
        }

        public OrderOpenResult OpenMarketOrd(string symbol, decimal lot, OrderSide side, OrderType type)
        {
            throw new NotImplementedException();
        }

        public OrderOpenResult OpenOrd(string symbol, decimal lot, OrderSide side, OrderType type, int price)
        {
            throw new NotImplementedException();
        }

        public bool OrderDelete(string id, string symbol, OrderType type, OrderSide side, decimal lot, decimal price)
        {
            return false;
        }

        public void Start()
        {
        }

        public void Stop(bool wait)
        {
        }

        public void Subscribe(string symbol, string id)
        {
        }

        public void Unsubscribe(string symbol, string id)
        {
        }
    }
#pragma warning restore CS0067
}
