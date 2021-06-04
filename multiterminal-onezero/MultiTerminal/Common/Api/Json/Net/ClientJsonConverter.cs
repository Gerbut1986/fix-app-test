using Newtonsoft.Json;

namespace Arbitrage.Api.Json.Net
{
    public class ClientJsonConverter : IClientJsonConverter
    {
        private readonly JsonSerializerSettings jsonSerializerOptions = new JsonSerializerSettings();
        public ClientJsonConverter()
        {
            jsonSerializerOptions.NullValueHandling = NullValueHandling.Ignore;
            jsonSerializerOptions.DateFormatString = "yyyy-MM-dd HH:mm:ss";
        }
        public T Deserialize<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data, jsonSerializerOptions);
        }

        public string Serialize(object data)
        {
            return JsonConvert.SerializeObject(data, jsonSerializerOptions);
        }
    }
}
