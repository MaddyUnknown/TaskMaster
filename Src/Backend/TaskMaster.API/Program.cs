using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskMaster.API.Auth;
using TaskMaster.API.BackgroundServices;
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
using TaskMaster.API.Middleware;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.JobTypes;
using TaskMaster.API.Models.Workers;
using TaskMaster.API.Publishers;
using TaskMaster.API.Queries;
using TaskMaster.API.Repositories;
using TaskMaster.API.Services;
using TaskMaster.API.Validation;

namespace TaskMaster.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Replace default logger provider with Serilog
            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext();
            });

            // Add services to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower));
                });

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });



            // Db Context
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
                options.EnableDetailedErrors();
            });

            // Authentication & authorization (pluggable: None | Oidc)
            builder.Services.AddTaskMasterAuth(builder.Configuration);

            // Application dependencies
            builder.Services.AddOptions<WorkerConfig>().Bind(builder.Configuration.GetSection("WorkerConfig"));


            builder.Services.AddSingleton<ISaveInterceptor, AuditDateTimeSaveInterceptor>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddTransient<IJobRepository, JobRepository>();
            builder.Services.AddTransient<IWorkerRepository, WorkerRepository>();
            builder.Services.AddTransient<IJobTypeRepository, JobTypeRepository>();

            builder.Services.AddTransient<IJobService, JobService>();
            builder.Services.AddTransient<IJobTypeService, JobTypeService>();
            builder.Services.AddTransient<IWorkerService, WorkerService>();
            builder.Services.AddTransient<IDashboardQuery, DashboardQuery>();
            builder.Services.AddTransient<IDashboardService, DashboardService>();

            // Event infrastructure
            builder.Services.AddTransient<IEventPublisher, EventPublisher>();
            builder.Services.AddTransient<IEventHandler<JobCreatedEvent>, SystemActivityEventHandler>();
            builder.Services.AddTransient<IEventHandler<JobAssignedEvent>, SystemActivityEventHandler>();
            builder.Services.AddTransient<IEventHandler<JobCompletedEvent>, SystemActivityEventHandler>();
            builder.Services.AddTransient<IEventHandler<JobFailedEvent>, SystemActivityEventHandler>();
            builder.Services.AddTransient<IEventHandler<WorkerRegisteredEvent>, SystemActivityEventHandler>();
            builder.Services.AddTransient<IEventHandler<WorkerInactiveEvent>, SystemActivityEventHandler>();
            builder.Services.AddTransient<IEventHandler<WorkerRemovedEvent>, SystemActivityEventHandler>();

            builder.Services.AddTransient<IEventHandler<IEnumerable<JobAssignedEvent>>, SystemActivityEventHandler>();
            builder.Services.AddTransient<IEventHandler<IEnumerable<JobCompletedEvent>>, SystemActivityEventHandler>();
            builder.Services.AddTransient<IEventHandler<IEnumerable<JobFailedEvent>>, SystemActivityEventHandler>();


            // Validators
            builder.Services.AddTransient<IValidator<CreateJob>, CreateJobValidator>();
            builder.Services.AddTransient<IValidator<CreateJobType>, CreateJobTypeValidator>();
            builder.Services.AddTransient<IValidator<RegisterWorker>, RegisterWorkerValidator>();
            builder.Services.AddTransient<IValidator<JobTypeRef>, JobTypeRefValidator>();

            // Swagger services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.ParameterLocation.Header,
                    Description = "Paste a valid OIDC access token to authorize requests."
                });

                options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
                {
                    [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                });
            });

            // Backgroup services
            builder.Services.AddHostedService<WorkerExpiryBackgroundService>();

            var app = builder.Build();

            // Enable Swagger
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Configure the HTTP request pipeline.
            app.UseHttpsRedirection();

            app.UseMiddleware<RequestContextMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMiddleware<ApiResponseStatusMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
