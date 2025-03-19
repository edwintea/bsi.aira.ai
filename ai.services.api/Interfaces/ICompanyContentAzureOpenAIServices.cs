using ai.services.api.Models;
using System.Threading.Tasks;

namespace ai.services.api.Interfaces
{
    public interface ICompanyContentAzureOpenAIServices
    {
        Task<ResponseEntity> CompanyContent(string value);
    }
}