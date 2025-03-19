using ai.services.api.Models;
using System.Threading.Tasks;

namespace ai.services.api.Interfaces
{
    public interface IResearchContentAzureOpenAIServices
    {
        Task<ResponseEntity> ResearchContent(string value);
    }
}