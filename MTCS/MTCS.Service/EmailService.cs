using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MTCS.Service
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, string companyName);
    }
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string companyName, string trackingCode)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");

            string body = $@"
        <html>
        <head>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f9f9f9;
                    padding: 20px;
                    color: #333;
                }}
                .container {{
                    background-color: #ffffff;
                    border-radius: 8px;
                    padding: 20px;
                    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                    max-width: 600px;
                    margin: auto;
                }}
                h2 {{
                    color: #4CAF50;
                }}
                .info {{
                    margin-top: 20px;
                    font-size: 16px;
                }}
                .link {{
                    margin-top: 30px;
                    display: inline-block;
                    padding: 10px 20px;
                    background-color: #4CAF50;
                    color: white;
                    text-decoration: none;
                    border-radius: 4px;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <h2>Thông báo đơn hàng mới</h2>
                <div class='info'>
                    <p><strong>Đơn hàng của bạn đã được tạo thành công.</p>
                    <p><strong>Tên khách hàng:</strong> {companyName}</p>
                    <p><strong>Mã đơn hàng:</strong> {trackingCode}</p>
                </div>
                <p>Khách hàng có thể truy cập trang bên dưới để tra cứu thông tin đơn hàng:</p>
                <a class='link' style='color: white' href='https://mtcs-fe.vercel.app/tracking-order' target='_blank'>Tra cứu đơn hàng</a>
            </div>
        </body>
        </html>";

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["SenderEmail"], smtpSettings["SenderName"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            using (var smtpClient = new SmtpClient(smtpSettings["Server"], int.Parse(smtpSettings["Port"])))
            {
                smtpClient.Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]);
                smtpClient.EnableSsl = bool.Parse(smtpSettings["EnableSsl"]);

                await smtpClient.SendMailAsync(mailMessage);
            }
        }
    }
}