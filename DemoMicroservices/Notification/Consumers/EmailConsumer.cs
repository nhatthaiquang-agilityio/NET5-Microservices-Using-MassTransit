using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;


namespace Notification.Consumers
{
    public class EmailConsumer : IConsumer<Messages.Commands.INotification>
    {
        private readonly ILogger<EmailConsumer> _logger;

        public EmailConsumer(ILogger<EmailConsumer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task Consume(ConsumeContext<Messages.Commands.INotification> context)
        {

            var data = context.Message;

            _logger.LogInformation(
                "Consume Notification Message: {NotificationId}, {NotificationType}, {NotificationContent}",
                data.NotificationId, data.NotificationType, data.NotificationContent);

            try
            {
                // TODO: call servive/task
                Task.Delay(2000);

            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to send Email. CorrelationId {NotificationId}, {NotificationType}, {NotificationContent}",
                    data.NotificationId, data.NotificationType, data.NotificationContent);
            }

            _logger.LogInformation("Consumed Order Message");

            return Task.CompletedTask;
        }

    }
}