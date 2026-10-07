using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Project.Base.Models.Emails;

namespace Project.Core.Emails
{
    public static class MailKitExtention
    {
        public static bool SendEmail(this MailKitRequest param)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(param.EmailFromName, param.EmailFrom));

            var tos = new InternetAddressList();
            var emailTos = param.EmailTo.Replace(" ", "").Split(';');
            foreach (var emailTo in emailTos)
            {
                tos.Add(new MailboxAddress(emailTo, emailTo));
            }

            email.To.AddRange(tos);
            email.Subject = param.Subject;
            email.Body = new TextPart(TextFormat.Html) { Text = param.Body };

            // send email
            using var smtp = new SmtpClient();

            if (!string.IsNullOrEmpty(param.SecureOption))
            {
                if (param.SecureOption == "None")
                {
                    smtp.Connect(param.SmtpHost, int.Parse(param.SmtpPort), SecureSocketOptions.None);
                }
                else
                {
                    smtp.Connect(param.SmtpHost, int.Parse(param.SmtpPort), SecureSocketOptions.SslOnConnect);
                }
            }

            if (!param.IsAnonymous)
            {
                smtp.Authenticate(param.SmtpUsername, param.SmtpPassword);
            }

            bool result;
            try
            {
                smtp.Send(email);
                result = true;
            }
            catch (Exception)
            {
                result = false;
                throw;
            }

            return result;
        }
    }
}
