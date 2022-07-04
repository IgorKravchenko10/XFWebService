using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XFServiceTest
{
    public class BasicHttpConnection
    {
        private HttpClient _httpClient;

        public string Address { get; set; }
        public string Prefix { get; set; }
        public bool IsSecure { get; set; }

        public BasicHttpConnection()
        {
            _httpClient = OnHttpClientCreating();
        }

        public BasicHttpConnection(string address) : this()
        {
            Address = address;
        }

        public BasicHttpConnection(string address, string prefix) : this(address)
        {
            Prefix = prefix;
        }

        private string ResolveHttps(string address)
        {
            if (address.StartsWith("https") || address.StartsWith("http"))
                return address;
            if (IsSecure)
                return $"https://{address}";
            return $"http://{address}";
        }

        protected virtual HttpClientHandler CreateHttpClientHandler()
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };
            return handler;
        }

        protected virtual HttpClient OnHttpClientCreating()
        {
            HttpClient httpClient = new HttpClient(CreateHttpClientHandler());
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }

        protected virtual HttpRequestMessage OnHttpRequestMessageCreating(HttpMethod httpMethod, string uri, Dictionary<string, string> headers = default)
        {
            HttpRequestMessage message = new HttpRequestMessage(httpMethod, uri);
            message.AddHeaders(headers);
            return message;
        }

        protected virtual string OnUriCreating(string action)
        {
            string uri = ResolveHttps(Address);

            if (!string.IsNullOrEmpty(Prefix))
                uri += $"/{Prefix}";
            if (!action.StartsWith("/"))
                uri += '/';
            uri += action;
            return uri;
        }

        public async Task<T> Get<T>(string action, CancellationToken cancellationToken, Dictionary<string, string> headers = default)
        {
            HttpMethod httpMethod = HttpMethod.Get;
            string uri = OnUriCreating(action);
            T proxyObject = default;
            using (HttpRequestMessage requestMessage = OnHttpRequestMessageCreating(httpMethod, uri, headers))
            {
                using (HttpResponseMessage response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                    {
                        proxyObject = await DeserializeJsonAsync<T>(stream, cancellationToken);
                    }
                }
            }
            return proxyObject;
        }

        public async Task<T> Post<T>(string action, object value, CancellationToken cancellationToken, Dictionary<string, string> headers = default)
        {
            HttpMethod httpMethod = HttpMethod.Post;
            string uri = OnUriCreating(action);
            T proxyObject = default;
            using (HttpRequestMessage requestMessage = OnHttpRequestMessageCreating(httpMethod, uri, headers))
            {
                string serializedObject = await JsonSerializeAsync(value);
                requestMessage.Content = new StringContent(serializedObject, Encoding.UTF8, "application/json");
                using (HttpResponseMessage response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                    {
                        proxyObject = await DeserializeJsonAsync<T>(stream, cancellationToken);
                    }
                }
            }
            return proxyObject;
        }

        protected static string JsonSerialize(object value) => JsonConvert.SerializeObject(value);

        protected static Task<string> JsonSerializeAsync(object value) =>
            Task.Run(() => JsonSerialize(value));

        protected static Task<T> DeserializeJsonAsync<T>(Stream stream, CancellationToken cancellationToken) =>
            Task.Run(() => DeserializeJson<T>(stream), cancellationToken);

        protected static T DeserializeJson<T>(Stream stream)
        {
            if (stream == null || stream.CanRead == false)
                return default;
            T result = default;
            using (StreamReader streamReader = new StreamReader(stream))
            {
                using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
                {
                    JsonSerializer jsonSerializer = new JsonSerializer();
                    result = jsonSerializer.Deserialize<T>(jsonTextReader);
                }
            }
            return result;
        }
    }

    public static class Extensions
    {
        public static string AddParameter(this string self, string parameterName, string parameterValue) =>
            $"{self}&{parameterName}={parameterValue}";

        public static string AddParameter(this string self, string parameterName, bool parameterValue) =>
            $"{self}&{parameterName}={parameterValue}";

        public static string AddParameter(this string self, string parameterName, int parameterValue) =>
            $"{self}&{parameterName}={parameterValue}";

        public static HttpRequestMessage AddHeaders(this HttpRequestMessage requestMessage, Dictionary<string, string> headers)
        {
            if (headers != null && headers.Count > 0)
                foreach (var header in headers)
                    requestMessage.Headers.Add(header.Key, header.Value);
            return requestMessage;
        }

        public static Dictionary<string, string> WithPair(this Dictionary<string, string> dictionary, string key, string value)
        {
            if (dictionary == null)
                dictionary = new Dictionary<string, string>();
            dictionary.Add(key, value);
            return dictionary;
        }
    }
}
