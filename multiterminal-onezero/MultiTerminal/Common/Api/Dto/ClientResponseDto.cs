using Arbitrage.Api.Enums;

namespace Arbitrage.Api.Dto
{
    public interface IClientResponseDto
    {
        ResponseStatus Status { get; set; }
        SubscriptionDto Subscription { get; set; }
    }
    public class ClientResponseDto<T> : IClientResponseDto
    {
        public ResponseStatus Status { get; set; }
        public SubscriptionDto Subscription { get; set; }
        public T Result { get; set; }
        public ClientResponseDto()
        {
            Status = ResponseStatus.Ok;
        }
    }
}