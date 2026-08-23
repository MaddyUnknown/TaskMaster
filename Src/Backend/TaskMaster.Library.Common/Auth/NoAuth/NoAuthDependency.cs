using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Common.Auth.NoAuth
{
    internal class NoAuthDependency : IAuthDependency
    {
        public void AddAuthServices(IServiceCollection services) { }
        public void AddAuthHttpClientHandler(IHttpClientBuilder httpClientBuilder) { }
    }
}
