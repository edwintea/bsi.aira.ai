using ai.services.api.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ai.services.api.Services
{
    public class ImageGenerationAzureOpenAIServices
    {
        private readonly HttpClient _http;
        private string azureImageGenEndpoint = Environment.GetEnvironmentVariable("azureImageGenEndpoint");
        private string azureImageGenApiKey = Environment.GetEnvironmentVariable("azureImageGenApiKey");
        private string azureImageGenVersion = Environment.GetEnvironmentVariable("azureImageGenVersion");
        private string azureImageGenDeploymentName = Environment.GetEnvironmentVariable("azureImageGenDeploymentName");
        private string connectionStringAzureStorage = Environment.GetEnvironmentVariable("blobStorageConnectionString");
        private readonly Lazy<TokenLogServices> tokenLogService;

        public ImageGenerationAzureOpenAIServices(HttpClient http)
        {
            _http = http;

            // Audit Log Service Initialization
            tokenLogService = new Lazy<TokenLogServices>(() => new TokenLogServices(connectionStringAzureStorage));
        }

        public async Task<ResponseEntity> ImageGeneration(string value)
        {
            string Endpoint = $"{azureImageGenEndpoint}/openai/deployments/{azureImageGenDeploymentName}/images/generations?api-version={azureImageGenVersion}";
            object[] messages = new object[0];
            object messageprompt = new object();

            var data = JsonConvert.DeserializeObject<List<dynamic>>(value);
            ResponseEntity _responseEntity = new ResponseEntity();

            foreach (var message in data)
            {
                foreach (var contentItem in message.content)
                {
                    messageprompt = new
                    {
                        prompt = $"{contentItem.text}",
                        n = 1,
                        style = "natural",
                        quality = "standard"
                    };
                }
            }

            try
            {
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("api-key", azureImageGenApiKey);

                var response = await _http.PostAsync(Endpoint,
                    new StringContent(JsonConvert.SerializeObject(messageprompt), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                    string messageContent = responseData.data[0].revised_prompt.ToString();
                    string urlContent = responseData.data[0].url.ToString();

                    string id = responseData.created.ToString();
                    string action = "AzureOpenAI-" + azureImageGenDeploymentName;
                    int input = 0;
                    int output = 0;

                    await tokenLogService.Value.CreateTokenLogAsync(id, action, input, output);

                    _responseEntity.isSuccess = true;
                    _responseEntity.Message = $"{response.StatusCode}";
                    _responseEntity.Result = messageContent;
                    _responseEntity.url = urlContent;
                }
                else
                {
                    _responseEntity.isSuccess = false;
                    _responseEntity.Message = $"{response.StatusCode}, {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                _responseEntity.isSuccess = false;
                _responseEntity.Message = $"Error: {ex.Message}";
            }

            return _responseEntity;
        }
    }
}