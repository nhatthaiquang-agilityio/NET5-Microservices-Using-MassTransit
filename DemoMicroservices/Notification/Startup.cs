using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MassTransit;
using MassTransit.Definition;
using Messages;
using Notification.Consumers;
using RabbitMQ.Client;
using Serilog;
using System;
using Messages.Commands;

namespace Notification
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

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            services.AddRazorPages();

            services.AddMassTransit(configureMassTransit =>
            {
                configureMassTransit.AddConsumer<PushNotificationConsumer>(config =>
                {
                    config.UseConcurrentMessageLimit(3);
                });

                if (Boolean.Parse(Configuration["UsingAmazonSQS"]))
                {
                    configureMassTransit.UsingAmazonSqs((context, configure) =>
                    {
                        ServiceBusConnectionConfig.ConfigureNodes(configuration, configure, "MessageBusSQS");

                        configure.ReceiveEndpoint(configuration["Queue"], receive =>
                        {
                            // disable the default topic binding
                            receive.ConfigureConsumeTopology = false;

                            receive.ConfigureConsumer<PushNotificationConsumer>(context);

                            receive.Subscribe<INotification>(m => 
                            {
                                receive.QueueSubscriptionAttributes["FilterPolicy"] = $"{{\"RoutingKey\": [\"{configuration["Topic"]}\"]}}";
                                // Using Environment tag
                                // m.TopicTags.Add("environment", "dev");
                            });
                        });

                    });
                }
                else
                {
                    configureMassTransit.UsingRabbitMq((context, configure) =>
                    {
                        configure.PrefetchCount = 4;

                        // Ensures the processor gets its own queue for any consumed messages
                        configure.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(true));

                        ServiceBusConnectionConfig.ConfigureNodes(configuration, configure, "MessageBus");

                        configure.ReceiveEndpoint(configuration["Queue"], receive =>
                        {
                            // turns off default fanout
                            receive.ConfigureConsumeTopology = false;

                            // a replicated queue to provide high availability and data safety. available in RMQ 3.8+
                            receive.SetQuorumQueue();

                            // enables a lazy queue for more stable cluster with better predictive performance.
                            // Please note that you should disable lazy queues if you require really high performance, if the queues are always short, or if you have set a max-length policy.
                            receive.SetQueueArgument("declare", "lazy");

                            receive.ConfigureConsumer<PushNotificationConsumer>(context);

                            receive.Bind(configuration["BindTopic"], eventMessage =>
                            {
                                eventMessage.RoutingKey = configuration["Topic"];
                                eventMessage.ExchangeType = ExchangeType.Topic;
                            });
                        });
                    });
                }
            });
            services.AddMassTransitHostedService();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
