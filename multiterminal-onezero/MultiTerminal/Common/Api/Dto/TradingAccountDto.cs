namespace Arbitrage.Api.Dto
{
    public class TradingAccountDto
    {
        public long Id { get; set; }
        public long SubscriptionId { get; set; }
        public string Broker { get; set; }
        public string Number { get; set; }
        public string Type { get; set; }
        public string Person { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public double Balance { get; set; }
        public double Equity { get; set; }
        public double TotalProfit { get; set; }
        public string Currency { get; set; }
        public string Terminal { get; set; }
        public string Server { get; set; }
        public string UpdateStamp { get; set; }
    }
    public class TradingAccountWithSubscriptionDto
    {
        public TradingAccountDto Account { get; set; }
        public SubscriptionDto Subscription { get; set; }
    }
}