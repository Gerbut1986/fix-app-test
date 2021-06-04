using Arbitrage.Api.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Arbitrage.Api.Clients
{
    public abstract class BaseClient
    {
        static bool RemoteCertificateValidator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool westernpips = false;
            if (sender is HttpWebRequest wr)
            {
                if (wr.Address!=null)
                {
                    if (wr.Address.Host == "westernpips.net") westernpips = true;
                }
            }
            if (westernpips)
            {
                string issuer = "CN=Sectigo RSA Domain Validation Secure Server CA, O=Sectigo Limited, L=Salford, S=Greater Manchester, C=GB";
                string subject = "CN=westernpips.net";
#if DEBUG
                if (certificate.Issuer == "CN=localhost" && certificate.Subject == "CN=localhost") return true;
#endif
                if (certificate.Issuer != issuer) return false;
                if (certificate.Subject != subject) return false;
            }
            return true;
        }
        public static void InitializeServicePointManager()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidator;
        }

        public IClientJsonConverter JsonConverter { get; private set; }
        public string Server { get; private set; }
        public string LastError { get; private set; }
        public BaseClient(string server, IClientJsonConverter jsonConverter)
        {
            Server = server;
            JsonConverter = jsonConverter;
        }
        protected abstract void OnRequest(object request);
        protected abstract void OnResponse(object response);
        private void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }
        private byte[] Zip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }
                return mso.ToArray();
            }
        }
        protected string Request(string url, object request, bool compressRequest)
        {
            StringBuilder result = new StringBuilder();
            try
            {
                LastError = "";
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Timeout = 60000;
                httpWebRequest.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip");
                if (request != null)
                {
                    OnRequest(request);
                    httpWebRequest.Method = "POST";
                    string body = JsonConverter.Serialize(request);
                    var bytes = Encoding.UTF8.GetBytes(body);
                    if (compressRequest)
                    {
                        httpWebRequest.Headers.Add(HttpRequestHeader.ContentEncoding, "gzip");
                        bytes = Zip(bytes);
                    }
                    using var writer = new BinaryWriter(httpWebRequest.GetRequestStream());
                    writer.Write(bytes, 0, bytes.Length);
                    writer.Flush();
                    writer.Close();
                }
                else
                {
                    httpWebRequest.Method = "GET";
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                var responseStream = httpResponse.GetResponseStream();
                if (httpResponse.ContentEncoding.ToLower().Contains("gzip"))
                {
                    responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                    using var streamReader = new StreamReader(responseStream);
                    result.Append(streamReader.ReadToEnd());
                }
                else
                {
                    using var streamReader = new StreamReader(responseStream);
                    result.Append(streamReader.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                LastError = e.ToString();
            }
            return result.ToString();
        }
        protected T JsonRequest<T>(string url, object request, bool compressRequest) where T : class
        {
            try
            {
                string response = Request(url, request,compressRequest);
                var res = JsonConverter.Deserialize<T>(response);
                if (res!=null) OnResponse(res);
                return res;
            }
            catch (Exception e)
            {
                if (!string.IsNullOrEmpty(LastError)) LastError += ";";
                if (string.IsNullOrEmpty(LastError)) LastError = "";
                LastError+=e.Message;
                return null;
            }
        }
        protected async Task<string> RequestAsync(string url, object request, bool compressRequest)
        {
            return await Task.Run(() =>
            {
                return Request(url, request,compressRequest);
            });
        }
        protected async Task<T> JsonRequestAsync<T>(string url, object request, bool compressRequest) where T : class
        {
            return await Task.Run(() =>
            {
                return JsonRequest<T>(url, request, compressRequest);
            });
        }
    }
}