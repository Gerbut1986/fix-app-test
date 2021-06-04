using System.Collections.Generic;

namespace Arbitrage.Api.Dto
{
    public class SubscriptionRegisterRequestDto : ClientRequestDto
    {
        public bool ResetSoftwareLocation { get; set; }
    }
    public class SubscriptionRegisterResponseDto : ClientResponseDto<object>
    {
    }
    public class SubscriptionLoginRequestDto : ClientRequestDto
    {
        public bool Extended { get; set; }
    }
    public class SubscriptionLoginResponseDto : ClientResponseDto<object>
    {
        public class BrokerInfoDto
        {
            public BrokerDto Broker { get; set; }
            public List<InstrumentDto> Instruments { get; set; }
            public List<BrokerFeatureExDto> BrokerFeatures { get; set; }
        }
        public List<BrokerInfoDto> Brokers { get; set; }
        public List<SubscriptionFeatureExDto> SubscriptionFeatures { get; set; }
    }
}