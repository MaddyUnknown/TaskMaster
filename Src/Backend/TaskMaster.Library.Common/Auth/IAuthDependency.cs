using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Library.Common.Auth.Oidc;
using TaskMaster.Library.Common.Configs;

namespace TaskMaster.Library.Common.Auth
{
    internal interface IAuthDependency
    {
        void AddAuthServices(IServiceCollection services);
        void AddAuthHttpClientHandler(IHttpClientBuilder httpClientBuilder);
    }
}
