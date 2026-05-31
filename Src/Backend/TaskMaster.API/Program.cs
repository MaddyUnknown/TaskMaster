using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json.Serialization;
using TaskMaster.API.Data;
using TaskMaster.API.Configs;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Repositories;
using TaskMaster.API.Services;

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
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Db Context
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });


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

            // Swagger services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Enable Swagger
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Configure the HTTP request pipeline.
            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
