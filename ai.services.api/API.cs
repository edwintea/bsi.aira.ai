using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using ai.services.api.Services;
using System.Net.Http;
using ai.services.api.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ai.services.api.Api
{
    public class API
    {
        private readonly HttpClient _http;
        private readonly Lazy<GeneralChatRAG> generalChatRAGServices;
        private readonly Lazy<GeneralChatAzureOpenAIServices> generalChatAzureOpenAIServices;
        private readonly Lazy<GeneralChatAzureOpenAIMiniServices> generalChatAzureOpenAIMiniServices;
        private readonly Lazy<GeneralChatClaudeServices> generalChatClaudeServices;
        private readonly Lazy<CompanyContentAzureOpenAIServices> companyContentAzureOpenAIServices;
        private readonly Lazy<ResearchContentAzureOpenAIServices> researchContentAzureOpenAIServices;
        private readonly Lazy<PerplexityServices> generalChatPerplexityServices;
        private readonly Lazy<ImageGenerationAzureOpenAIServices> imageGenerationAzureOpen;
        private readonly Lazy<ChatHistoryServices> chatHistoryServices;

        public API(HttpClient http)
        {
            _http = http;

            // General Chat Azure OpenAI Service Initialization
            generalChatRAGServices = new Lazy<GeneralChatRAG>(() => new GeneralChatRAG(http));

            // General Chat Azure OpenAI Service Initialization
            generalChatAzureOpenAIServices = new Lazy<GeneralChatAzureOpenAIServices>(() => new GeneralChatAzureOpenAIServices(http));

            // General Chat Azure OpenAI Mini Service Initialization
            generalChatAzureOpenAIMiniServices = new Lazy<GeneralChatAzureOpenAIMiniServices>(() => new GeneralChatAzureOpenAIMiniServices(http));

            // General Chat Claude Service Initialization
            generalChatClaudeServices = new Lazy<GeneralChatClaudeServices>(() => new GeneralChatClaudeServices(http));

            // Company Content Azure OpenAI Service Initialization
            companyContentAzureOpenAIServices = new Lazy<CompanyContentAzureOpenAIServices>(() => new CompanyContentAzureOpenAIServices(http));

            // Research Content Azure OpenAI Service Initialization
            researchContentAzureOpenAIServices = new Lazy<ResearchContentAzureOpenAIServices>(() => new ResearchContentAzureOpenAIServices(http));
            
            // General Chat Perplexity Service Initialization
            generalChatPerplexityServices = new Lazy<PerplexityServices>(() => new PerplexityServices(http));

            // Image Generation Azure OpenAI Service Initialization
            imageGenerationAzureOpen = new Lazy<ImageGenerationAzureOpenAIServices>(() => new ImageGenerationAzureOpenAIServices(http));

            // Azure Chat History Service Initialization
            chatHistoryServices = new Lazy<ChatHistoryServices>(() => new ChatHistoryServices(http));
        }

        [FunctionName("GeneralChat")]
        public async Task<IActionResult> GeneralChat(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string model = req.Query["model"];
            return new OkObjectResult(await generalChatAzureOpenAIServices.Value.GeneralChat(requestBody));
        }

        [FunctionName("GeneralChatMini")]
        public async Task<IActionResult> GeneralChatMini(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            return new OkObjectResult(await generalChatAzureOpenAIMiniServices.Value.GeneralChat(requestBody));
        }

        [FunctionName("GeneralChatClaude")]
        public async Task<IActionResult> GeneralChatClaude(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string model = req.Query["model"];
            return new OkObjectResult(await generalChatRAGServices.Value.GeneralChat(requestBody, model));
        }

        [FunctionName("CompanyContent")]
        public async Task<IActionResult> CompanyContent(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            return new OkObjectResult(await companyContentAzureOpenAIServices.Value.CompanyContent(requestBody));
        }

        [FunctionName("ResearchContent")]
        public async Task<IActionResult> ResearchContent(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            return new OkObjectResult(await researchContentAzureOpenAIServices.Value.ResearchContent(requestBody));
        }

        [FunctionName("GeneralChatPerplexity")]
        public async Task<IActionResult> GeneralChatPerplexity(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            return new OkObjectResult(await generalChatPerplexityServices.Value.GeneralChat(requestBody));
        }

        [FunctionName("ImageGeneration")]
        public async Task<IActionResult> ImageGeneration(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            return new OkObjectResult(await imageGenerationAzureOpen.Value.ImageGeneration(requestBody));
        }

        [FunctionName("SaveChatGroupHistory")]
        public async Task<IActionResult> SaveChatGroupHistory(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chatgroup/save")] HttpRequest req)
        {
            string fileName = req.Query["fileName"];
            if (string.IsNullOrEmpty(fileName))
                return new BadRequestObjectResult("File name is required.");

            string eid = req.Query["eid"];
            if (string.IsNullOrEmpty(eid))
                return new BadRequestObjectResult("Employee ID is required.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrEmpty(requestBody))
                return new BadRequestObjectResult("Chat history data is required.");
            
            return new OkObjectResult(await chatHistoryServices.Value.SaveChatGroupHistoryAsync(fileName, requestBody, eid));
        }

        [FunctionName("SaveChatItemHistory")]
        public async Task<IActionResult> SaveChatItemHistory(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chatitem/save")] HttpRequest req)
        {
            string fileName = req.Query["fileName"];
            if (string.IsNullOrEmpty(fileName))
                return new BadRequestObjectResult("File name is required.");

            string eid = req.Query["eid"];
            if (string.IsNullOrEmpty(eid))
                return new BadRequestObjectResult("Employee ID is required.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrEmpty(requestBody))
                return new BadRequestObjectResult("Chat history data is required.");

            return new OkObjectResult(await chatHistoryServices.Value.SaveChatItemHistoryAsync(fileName, requestBody, eid));
        }

        [FunctionName("DeleteChatGroupHistory")]
        public async Task<IActionResult> DeleteChatGroupHistory(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "chatgroup/delete")] HttpRequest req)
        {
            string fileName = req.Query["fileName"];
            if (string.IsNullOrEmpty(fileName))
                return new BadRequestObjectResult("File name is required.");

            string eid = req.Query["eid"];
            if (string.IsNullOrEmpty(eid))
                return new BadRequestObjectResult("Employee ID is required.");

            return new OkObjectResult(await chatHistoryServices.Value.DeleteChatGroupHistoryAsync(fileName, eid));
        }

        [FunctionName("DeleteChatITemHistory")]
        public async Task<IActionResult> DeleteChatItemHistory(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "chatitem/delete")] HttpRequest req)
        {
            string fileName = req.Query["fileName"];
            if (string.IsNullOrEmpty(fileName))
                return new BadRequestObjectResult("File name is required.");

            string eid = req.Query["eid"];
            if (string.IsNullOrEmpty(eid))
                return new BadRequestObjectResult("Employee ID is required.");

            return new OkObjectResult(await chatHistoryServices.Value.DeleteChatItemHistoryAsync(fileName, eid));
        }

        [FunctionName("GetChatGroupHistory")]
        public async Task<IActionResult> GetChatGroupHistory(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "chatgroup/get")] HttpRequest req)
        {
            string eid = req.Query["eid"];
            if (string.IsNullOrEmpty(eid))
                return new BadRequestObjectResult("Employee ID is required.");

            string serviceType = req.Query["serviceType"];

            string pageSize = req.Query["pageSize"];
            int take = 0;
            if (string.IsNullOrEmpty(pageSize))
                take = 15;
            else
                take = int.Parse(pageSize);

            string page = req.Query["page"];
            int takepage = 0;
            if (string.IsNullOrEmpty(page))
                takepage = 1;
            else
                takepage = int.Parse(page);

            return new OkObjectResult(await chatHistoryServices.Value.GetChatGroupHistoryListAsync(eid, serviceType, take, takepage));
        }

        [FunctionName("GetChatItemHistory")]
        public async Task<IActionResult> GetChatItemHistory(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "chatitem/get")] HttpRequest req)
        {
            string fileName = req.Query["fileName"];
            if (string.IsNullOrEmpty(fileName))
                return new BadRequestObjectResult("File name is required.");

            string eid = req.Query["eid"];
            if (string.IsNullOrEmpty(eid))
                return new BadRequestObjectResult("Employee ID is required.");

            return new OkObjectResult(await chatHistoryServices.Value.GetChatItemHistoryAsync(fileName, eid));
        }
    }
}
