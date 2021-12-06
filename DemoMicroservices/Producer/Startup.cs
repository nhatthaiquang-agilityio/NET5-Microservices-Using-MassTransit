using MassTransit;
using MassTransit.Definition;
using Messages;
using Messages.Commands;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using Serilog;
using System;

namespace Producer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json")
              .Build();

            Log.Logger = new Serilog.LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Producer", Version = "v1" });
            });

            services.AddMassTransit(configureMassTransit =>
            {
                configureMassTransit.UsingRabbitMq((context, configure) =>
                {
                    // Ensures the processor gets its own queue for any consumed messages
                    configure.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(true));
                    ServiceBusConnectionConfig.ConfigureNodes(configuration, configure, "MessageBus");

                    // name of the primary exchange
                    configure.Message<INotification>(e => e.SetEntityName("send-notification"));

                    // primary exchange type
                    configure.Publish<INotification>(e => e.ExchangeType = ExchangeType.Topic);

                    configure.Send<INotification>(e =>
                    {
                        e.UseRoutingKeyFormatter(context =>
                        {
                            return "notification.push";
                        });
                    });
                });
            });

            EndpointConvention.Map<Messages.Commands.Order>(new Uri(Configuration["EndpointConventionOrderMessage"]));

            services.AddMassTransitHostedService();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Producer v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
