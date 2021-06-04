namespace Arbitrage.Api.Json
{
    public interface IClientJsonConverter
    {
        string Serialize(object data);
        T Deserialize<T>(string data);
    }
}
