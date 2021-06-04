using System.Collections.Generic;

namespace Arbitrage.Api.Dto
{
    public class BrokerDto
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string DisplayName { get; set; }
    }
    public class BrokerWithFeaturesDto
    {
        public BrokerDto Broker { get; set; }
        public List<BrokerFeatureExDto> Features { get; set; }
    }
}
