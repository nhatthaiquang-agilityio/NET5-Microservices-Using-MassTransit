using MassTransit;
using MassTransit.Definition;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using EmailService.Consumers;
using RabbitMQ.Client;
using Serilog;
using System.Threading.Tasks;
using System;
using Messages.Commands;

namespace EmailService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var hostBuilder = CreateHostBuilder(args);

            var host = hostBuilder.Build();

            // host.Services.GetRequiredService<IBusControl>().Start();

            Task.Run(async () => await host.RunAsync()).GetAwaiter().GetResult();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json")
              .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.AddSerilog();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<EmailService>();

                    services.AddMassTransit(configureMassTransit =>
                    {
                        configureMassTransit.AddConsumer<EmailConsumer>(configureConsumer =>
                        {
                            configureConsumer.UseConcurrentMessageLimit(2);
                        });

                        if (Boolean.Parse(configuration["UsingAmazonSQS"]))
                        {
                            configureMassTransit.UsingAmazonSqs((context, configure) =>
                            {
                                ServiceBusConnectionConfig.ConfigureNodes(configuration, configure, "MessageBusSQS");

                                configure.ReceiveEndpoint(configuration["Queue"], receive =>
                                {
                                    // disable the default topic binding
                                    receive.ConfigureConsumeTopology = false;

                                    receive.ConfigureConsumer<EmailConsumer>(context);

                                    receive.PrefetchCount = 4;

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

                                    receive.ConfigureConsumer<EmailConsumer>(context);

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
                });
        }
    }
}
