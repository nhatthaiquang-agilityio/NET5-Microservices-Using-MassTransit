using MassTransit;
using Messages.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Producer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public NotificationController(ILogger<NotificationController> logger, IPublishEndpoint publishEndpoint)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            _logger.LogInformation("Get Notification API");
            return new List<string>();
        }

        [HttpPost]
        public async Task<IActionResult> Notification(Messages.Models.NotificationViewModel notificationModel)
        {
            _logger.LogInformation("Post Notification API");

            if (notificationModel != null)
            {
                var notify = new
                {
                    NotificationId = Guid.NewGuid(),
                    NotificationType = notificationModel.NotificationType,
                    NotificationDate = DateTime.Now,
                    NotificationContent = notificationModel.NotificationContent
                };

                try
                {
                    await _publishEndpoint.Publish<INotification>(notify);

                    _logger.LogInformation(
                       "Send to a message {NotificationId}, {NotificationType}",
                       notify.NotificationId, notify.NotificationType);

                    return Ok("Notification is sent.");
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Error Send Notification {NotificationId}, {NotificationType}",
                        notify.NotificationId, notify.NotificationType);
                }


            }

            return BadRequest();
        }
    }
}
