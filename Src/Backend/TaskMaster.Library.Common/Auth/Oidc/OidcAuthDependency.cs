using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Library.Common.Configs;

namespace TaskMaster.Library.Common.Auth.Oidc
{
    internal class OidcAuthDependency : IAuthDependency
    {
        public void AddAuthServices(IServiceCollection services)
        {
            services.AddTransient<BearerTokenDelegatingHandler>();
            services.AddSingleton<IAuthTokenProvider, AuthTokenProvider>();
        }

        public void AddAuthHttpClientHandler(IHttpClientBuilder httpClientBuilder)
        {
            httpClientBuilder.AddHttpMessageHandler<BearerTokenDelegatingHandler>();
        }
    }
}
