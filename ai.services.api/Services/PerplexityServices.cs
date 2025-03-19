using ai.services.api.Interfaces;
using ai.services.api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ai.services.api.Services
{
    public class PerplexityServices
    {
        private readonly HttpClient _http;
        private string perplexityEndpoint = Environment.GetEnvironmentVariable("perplexityEndpoint");
        private string perplexityKey = Environment.GetEnvironmentVariable("perplexityKey");
        // private string claudeVersion = Environment.GetEnvironmentVariable("claudeVersion");
        private string perplexityDeploymentName = Environment.GetEnvironmentVariable("perplexityDeploymentName");
        private string connectionStringAzureStorage = Environment.GetEnvironmentVariable("blobStorageConnectionString");
        private readonly Lazy<TokenLogServices> tokenLogService;

        public PerplexityServices(HttpClient http)
        {
            _http = http;

            // Audit Log Service Initialization
            tokenLogService = new Lazy<TokenLogServices>(() => new TokenLogServices(connectionStringAzureStorage));
        }

        public async Task<ResponseEntity> GeneralChat(string value)
        {
            string Endpoint = $"{perplexityEndpoint}/chat/completions";
            string responseMessage = "";
            object[] messages = new object[0];

            var data = JsonConvert.DeserializeObject<List<dynamic>>(value);
            ResponseEntity _responseEntity = new ResponseEntity();

            foreach (var message in data)
            {
                var newMessage = new object();
                var contentsource = new object();
                var contenttext = new object();
                int contentCount = message?.content?.Count ?? 0;

                foreach (var contentItem in message.content)
                {
                    if (message.role == "user" && contentItem.type == "image" && contentItem.source != null)
                    {
                        contentsource = new
                        {
                            type = $"{contentItem.type}",
                            source = new
                            {
                                type = $"{contentItem.source.type}",
                                media_type = $"{contentItem.source.media_type}",
                                data = $"{contentItem.source.data}" 
                            }
                        };
                    }
                    else if (message.role == "user" && contentItem.type == "document" && contentItem.source != null)
                    {
                        contentsource = new
                        {
                            type = $"{contentItem.type}",
                            source = new
                            {
                                type = $"{contentItem.source.type}",
                                media_type = $"{contentItem.source.media_type}",
                                data = $"{contentItem.source.data}"
                            }
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

                    if (contentCount > 1)
                    {
                        newMessage = new
                        {
                            role = $"{message.role}",
                            content = new object[] {
                                contentsource,
                                contenttext
                            }
                        };
                    }
                    else
                    {
                        newMessage = new
                        {
                            role = $"{message.role}", 
                            content = $"{contentItem.text}"
                        };
                    }
                }

                messages = messages.Concat(new object[]
                {
                    newMessage
                }).ToArray();
            }

            try
            {
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("Authorization","Bearer " + perplexityKey);

                var payload = new
                {
                    messages,
                    model = perplexityDeploymentName,
                    temperature = 0.8,
                    top_p = 0.95,
                    stream = false,
                    return_citations = true,
                    return_images = true,
                    return_related_questions = true,
                };

                var response = await _http.PostAsync(Endpoint,
                    new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    var citations = responseData.citations;

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
                    string action = "Perplexity-" + responseData.model.ToString();
                    int input = int.Parse(responseData.usage.prompt_tokens.ToString()) ?? 0;
                    int output = int.Parse(responseData.usage.completion_tokens.ToString()) ?? 0;

                    await tokenLogService.Value.CreateTokenLogAsync(id, action, input, output);

                    responseMessage = messageContent;

                    _responseEntity.isSuccess = true;
                    _responseEntity.Message = $"{response.StatusCode}";
                    _responseEntity.Result = responseMessage;
                    List<object> transformedCitations = new List<object>();
                    foreach (var citation in citations)
                    {
                        transformedCitations.Add(new { url = (string)citation });
                    }
                    _responseEntity.Citations = transformedCitations.ToArray();

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
