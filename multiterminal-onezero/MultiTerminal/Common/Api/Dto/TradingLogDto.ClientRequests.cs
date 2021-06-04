namespace Arbitrage.Api.Dto
{
    public class TradingLogCreateRequestDto : ClientRequestDto
    {
        public class LogInfo
        {
            public string Type { get; set; }
            public string Comment { get; set; }
            public string Content { get; set; }
            public string UpdateStamp { get; set; }
        }
        public class AccountInfo
        {
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
        public LogInfo Log { get; set; }
        public AccountInfo Account { get; set; }
    }
    public class TradingLogCreateResponseDto : ClientResponseDto<TradingLogDto>
    {
        public TradingAccountDto TradingAccount { get; set; }
    }
}