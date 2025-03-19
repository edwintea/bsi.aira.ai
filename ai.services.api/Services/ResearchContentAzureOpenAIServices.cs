using ai.services.api.Interfaces;
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
    public class ResearchContentAzureOpenAIServices : IResearchContentAzureOpenAIServices
    {
        private readonly HttpClient _http;
        private string azureOpenAIEndpoint = Environment.GetEnvironmentVariable("azureOpenAIEndpoint");
        private string azureOpenAIApiKey = Environment.GetEnvironmentVariable("azureOpenAIApiKey");
        private string azureOpenAIVersion = Environment.GetEnvironmentVariable("azureOpenAIVersion");
        private string azureOpenAIDeploymentName = Environment.GetEnvironmentVariable("azureOpenAIDeploymentName");
        private string connectionStringAzureStorage = Environment.GetEnvironmentVariable("blobStorageConnectionString");
        private string searchEndpoint = Environment.GetEnvironmentVariable("searchEndpoint");
        private string searchKey = Environment.GetEnvironmentVariable("searchKey");
        private string serviceName = Environment.GetEnvironmentVariable("serviceName");
        private string searchIndexResearch = Environment.GetEnvironmentVariable("searchIndexResearch");
        private string searchIndexCompany = Environment.GetEnvironmentVariable("searchIndexCompany");
        private readonly Lazy<TokenLogServices> tokenLogService;

        public ResearchContentAzureOpenAIServices(HttpClient http)
        {
            _http = http;

            // Audit Log Service Initialization
            tokenLogService = new Lazy<TokenLogServices>(() => new TokenLogServices(connectionStringAzureStorage));
        }

        public async Task<ResponseEntity> ResearchContent(string value)
        {
            string Endpoint = $"{azureOpenAIEndpoint}/openai/deployments/{azureOpenAIDeploymentName}/chat/completions?api-version=2024-02-15-preview";
            object[] messages = new object[0];
            object[] data_sources = new object[0];

            var data = JsonConvert.DeserializeObject<List<dynamic>>(value);
            ResponseEntity _responseEntity = new ResponseEntity();

            foreach (var message in data)
            {
                var newMessage = new object();

                foreach (var contentItem in message.content)
                {
                    newMessage = new
                    {
                        role = $"{message.role}",
                        content = $"{contentItem.text}"
                    };
                }

                messages = messages.Concat(new object[]
                {
                    newMessage
                }).ToArray();
            }

            var fields_mapping = new object();

            fields_mapping = new
            {
            };

            var authentication = new object();

            authentication = new
            {
                type = "api_key",
                key = searchKey
            };

            var parameters = new object();

            parameters = new
            {
                endpoint = searchEndpoint,
                index_name = searchIndexResearch,
                semantic_configuration = searchIndexResearch + "-semantic-configuration",
                query_type = "semantic",
                fields_mapping,
                in_scope = true,
                role_information = "You are an AI assistant that helps people find information.",
                filter = (string)null,
                strictness = 3,
                top_n_documents = 5,
                authentication
            };

            var data_sources_temp = new object();

            data_sources_temp = new
            {
                type = "azure_search",
                parameters,
                key = searchKey,
                indexName = searchIndexResearch
            };

            data_sources = data_sources.Concat(new object[]
            {
                data_sources_temp
            }).ToArray();

            try
            {
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("api-key", azureOpenAIApiKey);

                var payload = new
                {
                    messages,
                    past_messages = 10,
                    temperature = 0.7,
                    top_p = 0.95,
                    frequency_penalty = 0,
                    presence_penalty = 0,
                    max_tokens = 800,
                    stop = (string)null,
                    azureSearchEndpoint = searchEndpoint,
                    azureSearchKey = searchKey,
                    azureSearchIndexName = searchIndexResearch,
                    data_sources
                };

                var temp = JsonConvert.SerializeObject(payload);

                var response = await _http.PostAsync(Endpoint,
                    new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                    string messageContent = responseData.choices[0].message.content.ToString();
                    object[] citations = new object[0];
                    foreach (var item in responseData.choices[0].message.context.citations)
                    {
                        citations = citations.Concat(new object[]
                        {
                            item
                        }).ToArray();
                    }

                    string id = responseData.id.ToString();
                    string action = "AzureOpenAI-" + responseData.model.ToString();
                    int input = int.Parse(responseData.usage.prompt_tokens.ToString()) ?? 0;
                    int output = int.Parse(responseData.usage.completion_tokens.ToString()) ?? 0;

                    await tokenLogService.Value.CreateTokenLogAsync(id, action, input, output);

                    _responseEntity.isSuccess = true;
                    _responseEntity.Message = $"{response.StatusCode}";
                    _responseEntity.Result = messageContent;
                    _responseEntity.Citations = citations;
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
