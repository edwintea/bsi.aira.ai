using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static ai.services.api.Models.GeneralChatEntity;
using ai.services.api.Models;
using ai.services.api.Interfaces;
using static ai.services.api.Services.GeneralChatAzureOpenAIServices;

namespace ai.services.api.Services
{
    public class GeneralChatAzureOpenAIServices
    {
        private readonly HttpClient _http;
        private string azureOpenAIEndpoint = Environment.GetEnvironmentVariable("azureOpenAIEndpoint");
        private string azureOpenAIApiKey = Environment.GetEnvironmentVariable("azureOpenAIApiKey");
        private string azureOpenAIVersion = Environment.GetEnvironmentVariable("azureOpenAIVersion");
        private string azureOpenAIDeploymentName = Environment.GetEnvironmentVariable("azureOpenAIDeploymentName");
        private string connectionStringAzureStorage = Environment.GetEnvironmentVariable("blobStorageConnectionString");
        private readonly Lazy<TokenLogServices> tokenLogService;

        public GeneralChatAzureOpenAIServices(HttpClient http)
        {
            _http = http;

            // Audit Log Service Initialization
            tokenLogService = new Lazy<TokenLogServices>(() => new TokenLogServices(connectionStringAzureStorage));
        }

        public async Task<ResponseEntity> GeneralChat(string value)
        {
            string Endpoint = $"{azureOpenAIEndpoint}/openai/deployments/{azureOpenAIDeploymentName}/chat/completions?api-version={azureOpenAIVersion}";
            object[] messages = new object[0];

            var data = JsonConvert.DeserializeObject<List<dynamic>>(value);
            ResponseEntity _responseEntity = new ResponseEntity();

            foreach (var message in data)
            {
                var newMessage = new object();
                var contentimage = new object();
                var contenttext = new object();
                int contentLoop = 1;

                foreach (var contentItem in message.content)
                {
                    if (message.role == "user" && contentItem.type == "image_url" && contentItem.image_url != null)
                    {
                        contentimage = new
                        {
                            type = $"{contentItem.type}",
                            image_url = new { url = $"data:image/jpeg;base64,{contentItem.image_url.url}" }
                        };
                    }
                    else if (message.role == "user" && contentItem.type == "text")
                    {
                        contenttext = new
                        {
                            type = $"{contentItem.type}",
                            text = $"{contentItem.text}"
                        };
                    }
                    else if (message.role == "system" && contentItem.type == "text")
                    {
                        contenttext = new
                        {
                            type = $"{contentItem.type}",
                            text = $"{contentItem.text}"
                        };
                    }
                    else if (message.role == "assistant")
                    {
                        newMessage = new
                        {
                            role = $"{message.role}",
                            content = new object[] {
                                    contenttext
                                }
                        };
                    }

                    if (contentLoop > 1)
                    {
                        newMessage = new
                        {
                            role = $"{message.role}",
                            content = new object[] {
                                contentimage,
                                contenttext
                            }
                        };
                    }
                    else
                    {
                        newMessage = new
                        {
                            role = $"{message.role}",
                            content = new object[] {
                                contenttext
                            }
                        };
                    }

                    contentLoop++;
                }

                messages = messages.Concat(new object[]
                {
                    newMessage
                }).ToArray();
            }

            try
            {
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("api-key", azureOpenAIApiKey);

                var payload = new
                {
                    messages,
                    temperature = 0.7,
                    top_p = 0.95,
                    max_tokens = 3000,
                    stream = false
                };

                var response = await _http.PostAsync(Endpoint,
                    new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                    string messageContent = responseData.choices[0].message.content.ToString();

                    messages = messages.Concat(new object[]
                    {
                        new {
                            role = "assistant",
                            content = new object[] {
                                new { type = "text", text = messageContent }
                            }
                        }
                    }).ToArray();

                    string id = responseData.id.ToString();
                    string action = "AzureOpenAI-" + responseData.model.ToString();
                    int input = int.Parse(responseData.usage.prompt_tokens.ToString()) ?? 0;
                    int output = int.Parse(responseData.usage.completion_tokens.ToString()) ?? 0;

                    await tokenLogService.Value.CreateTokenLogAsync(id, action, input, output);

                    _responseEntity.isSuccess = true;
                    _responseEntity.Message = $"{response.StatusCode}";
                    _responseEntity.Result = messageContent;
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
