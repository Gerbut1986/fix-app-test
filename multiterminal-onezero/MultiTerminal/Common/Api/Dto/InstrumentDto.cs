namespace Arbitrage.Api.Dto
{
    public class InstrumentDto
    {
        public long Id { get; set; }
        public long BrokerId { get; set; }
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public int Digits { get; set; }
        public double Point { get; set; }
    }
}
