using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.API.Configs;
using TaskMaster.API.Data;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Repositories;
using TaskMaster.API.Services;

namespace TaskMaster.IntegrationTests
{
    public static class DependencyContainerBuilder
    {
        public static IConfiguration GetConfiguration() =>
           new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.testing.json", true, true)
               .AddUserSecrets<ConcurrencyTests>(optional: true)
               .AddEnvironmentVariables()
               .Build();

        public static ServiceProvider GetServicesProvider()
        {
            var configuration = GetConfiguration();

            var services = new ServiceCollection();

            Console.WriteLine(configuration);

            // Configuration
            services.AddSingleton(configuration);

            // Db Context
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("IntegrationTesting"));
            });


            // Application dependencies
            services.AddOptions<WorkerConfig>().Bind(configuration.GetSection("WorkerConfig"));

            services.AddSingleton<ISaveInterceptor, AuditDateTimeSaveInterceptor>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient<IJobRepository, JobRepository>();
            services.AddTransient<IWorkerRepository, WorkerRepository>();
            services.AddTransient<IJobTypeRepository, JobTypeRepository>();

            services.AddTransient<IJobService, JobService>();
            services.AddTransient<IJobTypeService, JobTypeService>();
            services.AddTransient<IWorkerService, WorkerService>();

            services.AddTransient<SqlServerFactory>();

            return services.BuildServiceProvider();
        }
    }
}
