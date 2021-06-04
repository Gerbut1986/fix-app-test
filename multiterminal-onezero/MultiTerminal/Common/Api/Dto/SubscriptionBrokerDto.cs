namespace Arbitrage.Api.Dto
{
    public class SubscriptionBrokerDto
    {
        public long Id { get; set; }
        public long SubscriptionId { get; set; }
        public long BrokerId { get; set; }
    }
    public class SubscriptionBrokerExDto
    {
        public SubscriptionBrokerDto SubscriptionBroker { get; set; }
        public BrokerDto Broker { get; set; }
    }
}