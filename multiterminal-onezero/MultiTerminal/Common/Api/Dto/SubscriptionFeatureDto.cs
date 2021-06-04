namespace Arbitrage.Api.Dto
{
    public class SubscriptionFeatureDto
    {
        public long Id { get; set; }
        public long SubscriptionId { get; set; }
        public long FeatureId { get; set; }
        public string Value { get; set; }
    }
    public class SubscriptionFeatureExDto
    {
        public SubscriptionFeatureDto SubscriptionFeature { get; set; }
        public FeatureDto Feature { get; set; }
    }
}
