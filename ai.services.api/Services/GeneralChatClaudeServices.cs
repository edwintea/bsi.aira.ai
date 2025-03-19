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
using static ai.services.api.Models.GeneralChatEntity;

namespace ai.services.api.Services
{
    public class GeneralChatClaudeServices
    {
        private readonly HttpClient _http;
        private string claudeEndpoint = Environment.GetEnvironmentVariable("claudeEndpoint");
        private string claudeApiKey = Environment.GetEnvironmentVariable("claudeApiKey");
        private string claudeVersion = Environment.GetEnvironmentVariable("claudeVersion");
        private string claudeDeploymentName = Environment.GetEnvironmentVariable("claudeDeploymentName");
        private string claudeBeta = Environment.GetEnvironmentVariable("claudeBeta");
        private string connectionStringAzureStorage = Environment.GetEnvironmentVariable("blobStorageConnectionString");
        private string maxTokenClaude = Environment.GetEnvironmentVariable("maxTokenClaude");
        private readonly Lazy<TokenLogServices> tokenLogService;
        private string modelDeploymentName;

        public GeneralChatClaudeServices(HttpClient http)
        {
            _http = http;

            // Audit Log Service Initialization
            tokenLogService = new Lazy<TokenLogServices>(() => new TokenLogServices(connectionStringAzureStorage));
        }

        public async Task<ResponseEntity> GeneralChat(string value, string model)
        {
            string Endpoint = $"{claudeEndpoint}";
            
            object[] messages = new object[0];
            var systemPersonalize = "Welcome to Eira! I am your personal assistant. How can I help you today?";

            var data = JsonConvert.DeserializeObject<List<dynamic>>(value);
            ResponseEntity _responseEntity = new ResponseEntity();

            foreach (var message in data)
            {
                var newMessage = new object();
                var contentsource = new object();
                var contenttext = new object();
                int contentLoop = 1;

                foreach (var contentItem in message.content)
                {
                    if (message.role == "user" && contentItem.type == "image" && contentItem.source != null)
                    {
                        string mediatype = contentItem.source.media_type;
                        string lowercasemediatype = mediatype.ToLower();
                        contentsource = new
                        {
                            type = $"{contentItem.type}",
                            source = new {
                                type = $"{contentItem.source.type}",
                                media_type = $"{lowercasemediatype}",
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
                    else if( message.role == "system")
                    {
                        systemPersonalize = $"{contentItem.text}";
                    }

                    if (contentLoop > 1)
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
                    else if (message.role != "system")
                    {
                        newMessage = new
                        {
                            role = $"{message.role}", 
                            content = $"{contentItem.text}"
                        };
                    }

                    contentLoop++;
                }

                if (message.role != "system")
                {
                    messages = messages.Concat(new object[]
                    {
                        newMessage
                    }).ToArray();
                }
            }
            
            try
            {
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("x-api-key", claudeApiKey);
                _http.DefaultRequestHeaders.Add("anthropic-version", claudeVersion);
                _http.DefaultRequestHeaders.Add("anthropic-beta", claudeBeta);
                
                modelDeploymentName = model switch
                {
                    "haiku" => "claude-3-5-haiku-latest",
                    "sonnet" => "claude-3-5-sonnet-latest",
                    _ => claudeDeploymentName,
                };

                var payload = new
                {
                    model = modelDeploymentName,
                    max_tokens = int.Parse(maxTokenClaude),
                    system = systemPersonalize,
                    messages
                };
                
                var response = await _http.PostAsync(Endpoint,
                    new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                    string messageContent = responseData.content[0].text.ToString();

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
                    string action = "Claude-" + responseData.model.ToString();
                    int input = int.Parse(responseData.usage.input_tokens.ToString()) ?? 0;
                    int output = int.Parse(responseData.usage.output_tokens.ToString()) ?? 0;

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
