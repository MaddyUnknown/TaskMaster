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
using TaskMaster.API.Events;
using TaskMaster.API.Handlers;
using TaskMaster.API.Interfaces;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.EventHandler;
using TaskMaster.API.Interfaces.Publisher;
using TaskMaster.API.Interfaces.Queries;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.JobTypes;
using TaskMaster.API.Models.Workers;
using TaskMaster.API.Publishers;
using TaskMaster.API.Queries;
using TaskMaster.API.Repositories;
using TaskMaster.API.Services;
using TaskMaster.API.Validation;
using TaskMaster.Test.IntegrationTests.Factories;

namespace TaskMaster.Test.IntegrationTests.Dependencies
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

            // Logging
            services.AddLogging();

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
            services.AddTransient<IDashboardQuery, DashboardQuery>();
            services.AddTransient<IDashboardService, DashboardService>();

            // Event infrastructure
            services.AddTransient<IEventPublisher, EventPublisher>();
            services.AddTransient<IEventHandler<JobCreatedEvent>, SystemActivityEventHandler>();
            services.AddTransient<IEventHandler<JobAssignedEvent>, SystemActivityEventHandler>();
            services.AddTransient<IEventHandler<JobCompletedEvent>, SystemActivityEventHandler>();
            services.AddTransient<IEventHandler<JobFailedEvent>, SystemActivityEventHandler>();
            services.AddTransient<IEventHandler<WorkerRegisteredEvent>, SystemActivityEventHandler>();
            services.AddTransient<IEventHandler<WorkerInactiveEvent>, SystemActivityEventHandler>();
            services.AddTransient<IEventHandler<WorkerRemovedEvent>, SystemActivityEventHandler>();

            // Validators
            services.AddTransient<IValidator<CreateJob>, CreateJobValidator>();
            services.AddTransient<IValidator<CreateJobType>, CreateJobTypeValidator>();
            services.AddTransient<IValidator<RegisterWorker>, RegisterWorkerValidator>();
            services.AddTransient<IValidator<JobTypeRef>, JobTypeRefValidator>();

            services.AddTransient<SqlServerFactory>();

            return services.BuildServiceProvider();
        }
    }
}
