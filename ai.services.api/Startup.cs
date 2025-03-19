using ai.services.api.Api;
using ai.services.api.Interfaces;
using ai.services.api.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace ai.services.api
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Register services and dependencies here

            builder.Services.AddHttpClient<API>();
        }
    }
}
