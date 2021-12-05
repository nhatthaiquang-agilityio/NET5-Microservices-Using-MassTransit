using System;

namespace Messages.Commands
{
    public interface INotification
    {
        public Guid NotificationId { get; set; }

        public string NotificationType { get; set; }

        public string NotificationContent { get; set; }

        public DateTime NotificationDate { get; set; }
    }
}
