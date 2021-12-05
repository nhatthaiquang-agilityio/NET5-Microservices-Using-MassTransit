using System;

namespace Messages.Commands
{
    public class Email
    {
        public Guid EmailId { get; set; }

        public string EmailAddress { get; set; }

        public string EmailContent { get; set; }
    }
}
