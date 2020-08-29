using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using MimeKit;
using RSecurityBackend.Models.Mail;
using System.Threading.Tasks;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// mail sender using MailKit
    /// </summary>
    public class MailKitEmailSender : IEmailSender
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        public MailKitEmailSender(IConfiguration configuration)
        {
            Configuration = configuration;
            Options = SmptConfig;
           
        }

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// options
        /// </summary>
        public SmptConfig Options { get; } //set only via Secret Manager


        private SmptConfig _SmptConfig = null;

        /// <summary>
        /// SmptConfig
        /// </summary>
        public SmptConfig SmptConfig
        {
            get
            {
                if (_SmptConfig == null)
                {
                    _SmptConfig =
                            new SmptConfig()
                            {
                                server = $"{Configuration.GetSection("SmptConfig")["Server"]}",
                                port = int.Parse($"{Configuration.GetSection("SmptConfig")["Port"]}"),
                                useSsl = bool.Parse($"{Configuration.GetSection("SmptConfig")["UseSsl"]}"),
                                smtpUsername = $"{ Configuration.GetSection("SmptConfig")["Username"] }",
                                smtpPassword = $"{Configuration.GetSection("SmptConfig")["Password"]}",
                                from = $"{ Configuration.GetSection("SmptConfig")["From"] }"

                            };
                }
                return _SmptConfig;

            }
        }

        /// <summary>
        /// send email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Execute(Options, subject, message, email);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task Execute(SmptConfig options, string subject, string message, string email)
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(options.from, options.from));
            mimeMessage.To.Add(new MailboxAddress(email, email));
            mimeMessage.Subject = subject;

            mimeMessage.Body = new TextPart("html")
            {
                Text = message
            };


            SmtpClient client = new SmtpClient();

            // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            client.Connect(options.server, options.port, options.useSsl);

            // Note: only needed if the SMTP server requires authentication
            client.Authenticate(options.smtpUsername, options.smtpPassword);

            client.Send(mimeMessage);
            client.Disconnect(true);
            client.Dispose();

            return Task.FromResult(0);
        }
    }
}
