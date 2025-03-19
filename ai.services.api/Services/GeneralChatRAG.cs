using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ai.services.api.Models;
using System.Net.Http.Headers;
using static ai.services.api.Models.GeneralChatEntity;
using System.IO;
using static ai.services.api.Models.ChatItemEntity;
using System.Text;
using System.Threading;
using System.Text.Json;

namespace ai.services.api.Services
{
    public class GeneralChatRAG
    {
        private readonly HttpClient _http;
        private string azureOpenAIEndpoint = Environment.GetEnvironmentVariable("azureOpenAIEndpoint");
        private string azureOpenAIApiKey = Environment.GetEnvironmentVariable("azureOpenAIApiKey");
        private string azureOpenAIVersion = Environment.GetEnvironmentVariable("azureOpenAIVersion");
        private string azureOpenAIDeploymentName = Environment.GetEnvironmentVariable("azureOpenAIDeploymentName");
        private string azureOpenAIDeploymentName4o3 = Environment.GetEnvironmentVariable("azureOpenAIDeploymentName4o3");

        private string azureOpenAIVersionMO1 = Environment.GetEnvironmentVariable("azureOpenAIVersionMO1");
        private string azureOpenAIDeploymentNameMO1 = Environment.GetEnvironmentVariable("azureOpenAIDeploymentNameMO1");

        private string claudeEndpoint = Environment.GetEnvironmentVariable("claudeEndpoint");
        private string claudeApiKey = Environment.GetEnvironmentVariable("claudeApiKey");
        private string claudeVersion = Environment.GetEnvironmentVariable("claudeVersion");
        private string claudeDeploymentName = Environment.GetEnvironmentVariable("claudeDeploymentName");
        private string claudeBeta = Environment.GetEnvironmentVariable("claudeBeta");
        private string maxTokenClaude = Environment.GetEnvironmentVariable("maxTokenClaude");

        private string connectionStringAzureStorage = Environment.GetEnvironmentVariable("blobStorageConnectionString");
        private readonly Lazy<TokenLogServices> tokenLogService;
        private string modelDeploymentName;

        public GeneralChatRAG(HttpClient http)
        {
            _http = http;

            // Audit Log Service Initialization
            tokenLogService = new Lazy<TokenLogServices>(() => new TokenLogServices(connectionStringAzureStorage));
        }

        public async Task<ResponseEntity> GeneralChat(string value, string model)
        {
            object[] messages = Array.Empty<object>();
            var data = JsonConvert.DeserializeObject<List<dynamic>>(value);
            bool isHasImge = false;
            bool isHasDoc = false;
            var base64mediatype = "";
            var base64data = "";
            var messagedoc = "";
            ResponseEntity _responseEntity = new ResponseEntity();

            foreach (var message in data)
            {
                var newMessage = new object();
                var contentimage = new object();
                var contentsource = new object();
                var contenttext = new object();
                int contentLoop = 1;

                foreach (var contentItem in message.content)
                {
                    if (message.role == "user" && contentItem.type == "document" && contentItem.source != null)
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
                        isHasDoc = true;
                        base64mediatype = $"{contentItem.source.media_type}";
                        base64data = $"{contentItem.source.data}";
                    }
                    else if (message.role == "user" && contentItem.type == "image" && contentItem.source != null)
                    {
                        string mediatype = contentItem.source.media_type;
                        string lowercasemediatype = mediatype.ToLower();
                        contentimage = new
                        {
                            type = $"{contentItem.type}_url",
                            image_url = new { url = $"data:{lowercasemediatype};{contentItem.source.type},{contentItem.source.data}" }
                        };
                        isHasImge = true;
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
                            //text = $"{contentItem.text}"
                            text = $"You are an AI assistant named Eira that helps people find information.You are also able to help users visualize data using HTML and JavaScript. The chart should be interactive and visually appealing. You should use the Chart.js library to implement this feature. The chart should automatically render upon the page load, and the design should be simple and clear to understand. Provide guidance to the user on how to save and view the HTML file in a web browser."
                        };
                    }
                    else if (message.role == "assistant")
                    {
                        contenttext = new
                        {
                            type = $"{contentItem.type}",
                            text = $"{contentItem.text}"
                        };
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
                        if (isHasDoc)
                        {
                            newMessage = new
                            {
                                role = $"{message.role}",
                                content = new object[] {
                                    contentsource,
                                    contenttext
                                }
                            };
                            messagedoc = $"{contentItem.text}";
                        }
                        else if (isHasImge)
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
            if (isHasDoc)
            {
                messages = messages.Skip(1).ToArray();
                _responseEntity = await GeneralChatClaude(messages, model);
                //var threadid = await CreateThread();
                //var assistanid = await UploadFile(base64mediatype, base64data);
                //var asstid = await CreateAssistant(assistanid);
                //var msgid = await ThreadChat(threadid, messagedoc);
                //var runid = await ThreadRun(threadid, asstid);
                //var statusid = await CheckStatusThread(threadid, runid);

                _responseEntity = await GeneralChatClaude(messages, model);
            }
            else if (isHasImge)
            {
                _responseEntity = await GeneralChatModel4O3(messages);
            }
            else
            {
                _responseEntity = await GeneralChatModel4O3(messages);
            }

            return _responseEntity;
        }

        public async Task<ResponseEntity> GeneralChatModel4O2(object[] messages)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            try
            {
                string Endpoint = $"{azureOpenAIEndpoint}/openai/deployments/{azureOpenAIDeploymentName}/chat/completions?api-version={azureOpenAIVersion}";
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("api-key", azureOpenAIApiKey);

                var payload = new
                {
                    messages,
                    temperature = 0.7,
                    top_p = 0.95,
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

        public async Task<ResponseEntity> GeneralChatModel4O3(object[] messages)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            try
            {
                string Endpoint = $"{azureOpenAIEndpoint}/openai/deployments/{azureOpenAIDeploymentName}/chat/completions?api-version={azureOpenAIVersion}";
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("api-key", azureOpenAIApiKey);

                var payload = new
                {
                    messages,
                    temperature = 0.7,
                    top_p = 0.95,
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

        public async Task<ResponseEntity> GeneralChatModelO1(object[] messages)
        {
            ResponseEntity _responseEntity = new ResponseEntity();

            try
            {
                string Endpoint = $"{azureOpenAIEndpoint}/openai/deployments/{azureOpenAIDeploymentNameMO1}/chat/completions?api-version={azureOpenAIVersionMO1}";

                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("api-key", azureOpenAIApiKey);

                var payload = new
                {
                    messages
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

        public async Task<ResponseEntity> GeneralChatClaude(object[] messages, string model)
        {
            ResponseEntity _responseEntity = new ResponseEntity();

            try
            {
                string Endpoint = $"{claudeEndpoint}";
                var systemPersonalize = "Welcome to Eira! I am your personal assistant. How can I help you today?";

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

        public async Task<string> CreateThread()
        {
            var apiversion = "2024-05-01-preview";
            ResponseEntity _responseEntity = new ResponseEntity();

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, $"{azureOpenAIEndpoint}/openai/threads?api-version={apiversion}");
                request.Headers.Add("api-key", azureOpenAIApiKey);
                var content = new StringContent(string.Empty);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                    _responseEntity.isSuccess = true;
                    _responseEntity.Message = $"{response.StatusCode}";
                    _responseEntity.id = $"{responseData.id}";
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

            return _responseEntity.id;
        }

        public async Task<string> UploadFile(string mediatype, string data)
        {
            var apiversion = "2024-05-01-preview";
            ResponseEntity _responseEntity = new ResponseEntity();

            try
            {
                byte[] fileBytes = Convert.FromBase64String(data);

                using (MemoryStream stream = new MemoryStream(fileBytes))
                {
                    StreamContent streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(mediatype);

                    var client = new HttpClient();
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{azureOpenAIEndpoint}/openai/files?api-version={apiversion}");
                    request.Headers.Add("api-key", azureOpenAIApiKey);
                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent("assistants"), "purpose");
                    content.Add(streamContent, "file", "upload data for assistant");
                    request.Content = content;
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                        _responseEntity.isSuccess = true;
                        _responseEntity.Message = $"{response.StatusCode}";
                        _responseEntity.id = $"{responseData.id}";
                    }
                    else
                    {
                        _responseEntity.isSuccess = false;
                        _responseEntity.Message = $"{response.StatusCode}, {response.ReasonPhrase}";
                    }
                }

            }
            catch (Exception ex)
            {
                _responseEntity.isSuccess = false;
                _responseEntity.Message = $"Error: {ex.Message}";
            }

            return _responseEntity.id;
        }

        public async Task<string> CreateAssistant(string fileids)
        {
            var apiversion = "2024-08-01-preview";
            ResponseEntity _responseEntity = new ResponseEntity();

            try
            {
                string content = $@"
                {{
                    ""instructions"": ""You are an AI assistant that can write code to help answer math questions."",
                    ""tools"": [
                        {{
                            ""type"": ""code_interpreter""
                        }}
                    ],
                    ""model"": ""gpt-4o-2-global"",
                    ""tool_resources"": {{
                        ""code_interpreter"": {{
                            ""file_ids"": [
                                ""{fileids}""
                            ]
                        }}
                    }}
                }}";

                var stringContent = new StringContent(content, Encoding.UTF8, "application/json");

                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, $"{azureOpenAIEndpoint}/openai/assistants?api-version={apiversion}");
                request.Headers.Add("api-key", azureOpenAIApiKey);
                request.Content = stringContent;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                Console.WriteLine(await response.Content.ReadAsStringAsync());


                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                    _responseEntity.isSuccess = true;
                    _responseEntity.Message = $"{response.StatusCode}";
                    _responseEntity.id = $"{responseData.id}";
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

            return _responseEntity.id;
        }

        public async Task<string> ThreadChat(string threadid, string message)
        {
            var apiversion = "2024-05-01-preview";
            ResponseEntity _responseEntity = new ResponseEntity();

            try
            {
                string role = "user";
                string userContent = message;
                string content = $@"
                {{
                    ""role"": ""{role}"",
                    ""content"": ""{userContent}""
                }}";

                var stringContent = new StringContent(content, Encoding.UTF8, "application/json");

                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, $"{azureOpenAIEndpoint}/openai/threads/{threadid}/messages?api-version={apiversion}");
                request.Headers.Add("api-key", azureOpenAIApiKey);
                request.Content = stringContent;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                    _responseEntity.isSuccess = true;
                    _responseEntity.Message = $"{response.StatusCode}";
                    _responseEntity.id = $"{responseData.id}";
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

            return _responseEntity.id;
        }

        public async Task<string> ThreadRun(string threadid, string asstid)
        {
            var apiversion = "2024-05-01-preview";
            ResponseEntity _responseEntity = new ResponseEntity();

            try
            {
                string content = $@"
                {{
                    ""assistant_id"": ""{asstid}""
                }}";

                var stringContent = new StringContent(content, Encoding.UTF8, "application/json");

                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, $"{azureOpenAIEndpoint}/openai/threads/{threadid}/runs?api-version={apiversion}");
                request.Headers.Add("api-key", azureOpenAIApiKey);
                request.Content = stringContent;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                    _responseEntity.isSuccess = true;
                    _responseEntity.Message = $"{response.StatusCode}";
                    _responseEntity.id = $"{responseData.id}";
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

            return _responseEntity.id;
        }

        public async Task<string> CheckStatusThread(string threadid, string runid)
        {
            var apiversion = "2024-05-01-preview";
            ResponseEntity _responseEntity = new ResponseEntity();

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{azureOpenAIEndpoint}/openai/threads/{threadid}/runs/{runid}?api-version={apiversion}");
                request.Headers.Add("api-key", azureOpenAIApiKey);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                    _responseEntity.isSuccess = true;
                    _responseEntity.Message = $"{response.StatusCode}";
                    _responseEntity.id = $"{responseData.id}";
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

            return _responseEntity.id;
        }

        public async Task<string> ResultThread(string threadid, string runid)
        {
            var apiversion = "2024-05-01-preview";
            ResponseEntity _responseEntity = new ResponseEntity();

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{azureOpenAIEndpoint}/openai/threads/{threadid}/messages?api-version={apiversion}");
                request.Headers.Add("api-key", azureOpenAIApiKey);
                var content = new StringContent(string.Empty);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = "";
                    var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                    // Parse JSON
                    var jsonObject = JsonDocument.Parse(jsonString);

                    // Get last_id
                    string lastId = jsonObject.RootElement.GetProperty("last_id").GetString();

                    // Find data by id = last_id
                    var dataArray = jsonObject.RootElement.GetProperty("data").EnumerateArray();
                    var lastData = dataArray.FirstOrDefault(item => item.GetProperty("id").GetString() == lastId);

                    if (lastData.ValueKind != JsonValueKind.Undefined)
                    {
                        // Extract content.text.value
                        var lastContent = lastData.GetProperty("content")
                            .EnumerateArray()
                            .Select(content => content.GetProperty("text").GetProperty("value").GetString())
                            .FirstOrDefault();

                        Console.WriteLine($"Data for last_id ({lastId}):");
                        Console.WriteLine($"Content: {lastContent}");
                    }
                    else
                    {
                        Console.WriteLine($"No data found for last_id: {lastId}");
                    }
                    _responseEntity.isSuccess = true;
                    _responseEntity.Message = $"{response.StatusCode}";
                    _responseEntity.id = $"{responseData.id}";
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

            return _responseEntity.id;
        }
    }
}
