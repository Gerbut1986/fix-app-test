using Arbitrage.Api.Enums;
using System;
using System.Collections.Generic;

namespace Arbitrage.Api.Dto
{
    public class SubscriptionDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string Login { get; set; }
        public string ComputerId { get; set; }
        public string IpAddress { get; set; }
        public DateTime SubscriptionTime { get; set; }
        public SubscriptionStatus Status { get; set; }
        public string SerialNumber { get; set; }
        public int ClientVersion { get; set; }
    }
    public class SubscriptionWithFeaturesAndBrokersDto
    {
        public SubscriptionDto Subscription { get; set; }
        public ProductDto Product { get; set; }
        public List<SubscriptionFeatureExDto> Features { get; set; }
        public List<SubscriptionBrokerExDto> Brokers { get; set; }
    }
}