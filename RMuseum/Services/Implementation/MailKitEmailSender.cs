using RSecurityBackend.Models.Mail;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    public class MailKitEmailSender : MailKitEmailSenderBase
    {
        /// <summary>
        /// SmptConfig
        /// </summary>
        public override SmptConfig SmptConfig
        {
            get
            {
                return
                    new SmptConfig()
                    {
                        port = 25,
                        server = "ganjgah.ir",
                        smtpUsername = "noreply@ganjgah.ir",
                        smtpPassword = "sahY%6&$OnD67i0978",
                        from = "noreply@ganjgah.ir",
                        useSsl = false
                    };
            }
        }
    }

}
