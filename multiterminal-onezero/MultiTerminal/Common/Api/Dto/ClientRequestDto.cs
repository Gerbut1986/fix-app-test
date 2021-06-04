namespace Arbitrage.Api.Dto
{
    public class ClientRequestDto
    {
        public string ProductCode { get; set; }
        public string Login { get; set; }
        public string ComputerId { get; set; }
        public string SerialNumber { get; set; }
        public int ClientVersion { get; set; }
    }
}
