using Arbitrage.Api.Dto;
using Arbitrage.Api.Json;
using System.Threading.Tasks;

namespace Arbitrage.Api.Clients
{
    public partial class Client : BaseClient
    {
        public string ProductCode { get; private set; }
        public string Login { get; private set; }
        public string ComputerId { get; private set; }
        public string SerialNumber { get; private set; }
        public int ClientVersion { get; private set; }
        public Client(string server, IClientJsonConverter jsonConverter)
            :base(server,jsonConverter)
        {
        }
        public Client(string server, IClientJsonConverter jsonConverter, string productCode, string login, string computerId, string serialNumber,int clientVersion) 
            :base(server,jsonConverter)
        {
            ProductCode = productCode;
            Login = login;
            ComputerId = computerId;
            SerialNumber = serialNumber;
            ClientVersion = clientVersion;
        }
        protected override void OnRequest(object request)
        {
            if (request is ClientRequestDto subscriptionDto)
            {
                subscriptionDto.ComputerId = ComputerId;
                subscriptionDto.Login = Login;
                subscriptionDto.ProductCode = ProductCode;
                subscriptionDto.SerialNumber = SerialNumber;
                subscriptionDto.ClientVersion = ClientVersion;
            }
        }
        protected override void OnResponse(object response)
        {
        }
        public async Task<string> Version()
        {
            return await RequestAsync(Server + "/api/v1/version", null, false);
        }
        public async Task<SubscriptionRegisterResponseDto> SubscriptionRegister(bool resetSoftwareLocation)
        {
            return await JsonRequestAsync<SubscriptionRegisterResponseDto>(Server + "/api/v1/subscription/register", new SubscriptionRegisterRequestDto() { ResetSoftwareLocation = resetSoftwareLocation }, false);
        }
        public async Task<SubscriptionLoginResponseDto> SubscriptionLogin(bool extended)
        {
            return await JsonRequestAsync<SubscriptionLoginResponseDto>(Server + "/api/v1/subscription/login", new SubscriptionLoginRequestDto() { Extended = extended }, false);
        }
        public async Task<TradingLogCreateResponseDto> CreateTradingLog(TradingLogCreateRequestDto.AccountInfo account, TradingLogCreateRequestDto.LogInfo log)
        {
            return await JsonRequestAsync<TradingLogCreateResponseDto>(Server + "/api/v1/tradinglog/create", new TradingLogCreateRequestDto()
            {
                Log=log,
                Account=account
            },true);
        }
    }
}