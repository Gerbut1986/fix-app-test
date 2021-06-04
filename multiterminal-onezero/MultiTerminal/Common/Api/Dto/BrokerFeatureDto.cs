namespace Arbitrage.Api.Dto
{
    public class BrokerFeatureDto
    {
        public long Id { get; set; }
        public long BrokerId { get; set; }
        public long FeatureId { get; set; }
        public string Value { get; set; }
    }
    public class BrokerFeatureExDto
    {
        public BrokerFeatureDto BrokerFeature { get; set; }
        public FeatureDto Feature { get; set; }
    }
}