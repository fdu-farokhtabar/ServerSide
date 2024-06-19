using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Email
{
    public class EmailProvider
    {
        /// <summary>
        /// price-list-email-setting
        /// </summary>
        /// <param name="SettingKey"></param>
        /// <param name="subject"></param>
        /// <param name="To"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public async Task SendMailAsync(EmailSetting Setting, string subject, string To, string body, List<string> AttachmentsPath = null, string cc = "", string bcc = "")
        {
            MailMessage mailMessage = new()
            {
                From = new MailAddress(Setting.From),
                Subject = subject,
                IsBodyHtml = true,
                Body = body,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };
            if (AttachmentsPath?.Count > 0)
            {
                foreach (var AttachmentPath in AttachmentsPath)
                    mailMessage.Attachments.Add(new Attachment(AttachmentPath));
            }

            mailMessage.To.Add(To);

            if (!string.IsNullOrWhiteSpace(cc))
                mailMessage.CC.Add(cc);

            if (!string.IsNullOrWhiteSpace(bcc))
                mailMessage.Bcc.Add(bcc);

            using var client = new SmtpClient(Setting.Host);
            client.Credentials = new NetworkCredential(Setting.UserName, Setting.Password);
            client.Port = Setting.Port;
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            try
            {
                await client.SendMailAsync(mailMessage);
            }
            catch (Exception Ex)
            {
                throw new Exception("Cannot send the email.");
            }
        }
    }
    public class EmailSetting
    {
        public string From { get; set; }
        public string Bcc { get; set; }
        public string Cc { get; set; }
        public string SubjectTemplate { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string BodyTemplate { get; set; }
    }
}
