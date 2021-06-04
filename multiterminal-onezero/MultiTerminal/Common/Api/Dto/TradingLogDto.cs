using System;

namespace Arbitrage.Api.Dto
{
    public class TradingLogDto
    {
        public long Id { get; set; }
        public long TradingAccountId { get; set; }
        public string Type { get; set; }
        public DateTime Time { get; set; }
        public string Comment { get; set; }
        public string Content { get; set; }
        public string UpdateStamp { get; set; }
    }
    public class TradingLogWithAccountDto
    {
        public TradingLogDto Log { get; set; }
        public TradingAccountDto Account { get; set; }
    }
}