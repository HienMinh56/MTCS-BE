﻿using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace MTCS.Service
{
    public interface IEmailService
    {
        Task SendAccountRegistration(string to, string subject, string fullName, string email, string password);
        Task SendEmailAsync(string to, string subject, string body, string companyName);
        Task SendEmailCancelAsync(string to, string subject, string body, string companyName);
        Task SendEmailContractExpirationAsync(string to, string contractDate, string expirationDate, string companyName, string contractID, DateOnly? signedTime);
        Task SendOrderCompletionToCustomer(string to, string subject, string companyName, string trackingCode, TimeOnly? completeTime);
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

        public async Task SendEmailCancelAsync(string to, string subject, string companyName, string trackingCode)
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
                <h2>Thông báo về việc hủy đơn hàng</h2>
                <div class='info'>
                    <p>Kính gửi Quý khách,</p>
                    <p>Chúng tôi đã tiếp nhận và xác nhận yêu cầu huỷ đơn hàng của Quý khách.</p>
                    <p><strong>Tên khách hàng:</strong> {companyName}</p>
                    <p><strong>Mã đơn hàng:</strong> {trackingCode}</p>
                    <p>Rất mong được phục vụ Quý khách trong những lần tiếp theo.</p>
                </div>
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


        public async Task SendEmailContractExpirationAsync(string to, string contractDate, string expirationDate, string companyName, string contractID, DateOnly? signedTime)
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
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Thông báo hợp đồng vận tải sắp hết hạn</h2>
                    <div class='info'>
                        <p>Kính gửi: {companyName}</p>
                        <p>Chúng tôi xin thông báo hợp đồng vận tải của bạn sắp hết hạn.</p>
                        <p>Mã hợp đồng: {contractID}</p>
                        <p>Ngày ký: {signedTime}</p>
                        <p>Ngày hết hạn vào ngày {expirationDate}.</p>
                        <p>Vui lòng liên hệ với chúng tôi để trao đổi về việc gia hạn hoặc ký kết hợp đồng vận tải mới.</p>
                        <p>Chân thành cảm ơn sự hợp tác của quý công ty.</p>
                        <p>Trân trọng.</p>
                    </div>
                </div>
            </body>
            </html>";

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["SenderEmail"], smtpSettings["SenderName"]),
                Subject = "Thông báo hết hạn hợp đồng vận tải",
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

        public async Task SendAccountRegistration(string to, string subject, string fullName, string email, string password)
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
                <h2>Thông báo tạo tài khoản nội bộ cho nhân viên mới - {fullName}</h2>
                <div class='info'>
                    <p>Kính gửi {fullName},</p>
                    <p>Chúng tôi xin thông báo rằng tài khoản nội bộ của bạn đã được tạo. Dưới đây là thông tin đăng nhập:</p>
                    <p>Email đăng nhập: {email}</p>
                    <p>Mật khẩu: {password}</p>
                    <p>Link truy cập trang web: <a href='https://mtcs-fe.vercel.app' target='_blank'>https://mtcs-fe.vercel.app</a></p>
                </div>
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

        public async Task SendOrderCompletionToCustomer(string to, string subject, string companyName, string trackingCode, TimeOnly? completeTime)
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
                <h2>Thông báo đơn hàng đã được giao thành công</h2>
                <div class='info'>
                    <p>Kính gửi khách hàng: {companyName},</p>
                    <p>Chúng tôi xin thông báo rằng đơn hàng {trackingCode} của bạn đã được giao thành công vào lúc {completeTime}.</p>
                    <p>Cảm ơn bạn đã tin tưởng và lựa chọn dịch vụ của chúng tôi. Nếu bạn có bất kỳ thắc mắc hoặc cần hỗ trợ thêm, xin vui lòng liên hệ với chúng tôi qua email này.</p>
                    <p>Chân thành cảm ơn sự tin tưởng của quý công ty.</p>
                    <p>Trân trọng.</p>
                </div>
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