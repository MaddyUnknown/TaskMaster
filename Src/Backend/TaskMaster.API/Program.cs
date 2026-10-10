using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskMaster.API.Auth;
using TaskMaster.API.BackgroundServices;
using TaskMaster.API.Data;
using TaskMaster.API.DependencyInjection;
using TaskMaster.API.Middleware;
using TaskMaster.API.Serialization;

namespace TaskMaster.API
{
    public class Program
    {
        public static async Task Main(string[] args)
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
                    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
                });

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });



            // Db Context (engine selected via Database:Provider)
            builder.Services.AddTaskMasterPersistence(builder.Configuration);

            // Authentication & authorization (pluggable: None | Oidc)
            builder.Services.AddTaskMasterAuth(builder.Configuration);

            // Application dependencies (shared with the integration test host)
            builder.Services.AddTaskMasterApplication(builder.Configuration);

            // Backgroup services
            builder.Services.AddHostedService<WorkerExpiryBackgroundService>();

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

            var app = builder.Build();

            // Bring the schema up to date before anything can use it. Runs ahead of the hosted services, which start with app.Run() below.
            await app.ApplyPendingMigrationsAsync(app.Logger);

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
