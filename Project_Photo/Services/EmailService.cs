using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Project_Photo.ViewModels;
using System;
using System.Threading.Tasks;

namespace Project_Photo.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettingsViewModel _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettingsViewModel> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetUrl, string verificationCode)
        {
            var subject = "重置您的密碼 - Project Photo";

            var htmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            line-height: 1.6;
                            color: #333;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 0 auto;
                            padding: 20px;
                        }}
                        .header {{
                            background-color: #066fd1;
                            color: white;
                            padding: 20px;
                            text-align: center;
                            border-radius: 5px 5px 0 0;
                        }}
                        .content {{
                            background-color: #f9f9f9;
                            padding: 30px;
                            border: 1px solid #ddd;
                            border-radius: 0 0 5px 5px;
                        }}
                        .button {{
                            display: inline-block;
                            padding: 12px 30px;
                            background-color: #066fd1;
                            color: white;
                            text-decoration: none;
                            border-radius: 5px;
                            margin: 20px 0;
                        }}
                        .code {{
                            font-size: 24px;
                            font-weight: bold;
                            color: #066fd1;
                            letter-spacing: 3px;
                            padding: 10px;
                            background-color: #e9ecef;
                            border-radius: 5px;
                            display: inline-block;
                        }}
                        .footer {{
                            margin-top: 20px;
                            padding-top: 20px;
                            border-top: 1px solid #ddd;
                            font-size: 12px;
                            color: #666;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>密碼重置請求</h1>
                        </div>
                        <div class='content'>
                            <h2>您好！</h2>
                            <p>我們收到了您重置密碼的請求。請點擊下方按鈕來重置您的密碼：</p>
            
                            <div style='text-align: center;'>
                                <a href='{resetUrl}' class='button'>重置密碼</a>
                            </div>
            
                            <p>或者您也可以使用以下驗證碼：</p>
                            <div style='text-align: center;'>
                                <span class='code'>{verificationCode}</span>
                            </div>
            
                            <p><strong>此連結將在 1 小時後失效。</strong></p>
            
                            <p>如果您沒有請求重置密碼，請忽略此郵件。您的帳號仍然是安全的。</p>
            
                            <div class='footer'>
                                <p>此為系統自動發送的郵件，請勿直接回覆。</p>
                                <p>&copy; 2024 Project Photo. All rights reserved.</p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "歡迎加入 Project Photo！";

            var htmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            line-height: 1.6;
                            color: #333;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 0 auto;
                            padding: 20px;
                        }}
                        .header {{
                            background-color: #066fd1;
                            color: white;
                            padding: 20px;
                            text-align: center;
                            border-radius: 5px 5px 0 0;
                        }}
                        .content {{
                            background-color: #f9f9f9;
                            padding: 30px;
                            border: 1px solid #ddd;
                            border-radius: 0 0 5px 5px;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>歡迎加入 Project Photo！</h1>
                        </div>
                        <div class='content'>
                            <h2>親愛的 {userName}，</h2>
                            <p>感謝您註冊 Project Photo！我們很高興您加入我們的社群。</p>
                            <p>現在您可以開始使用我們的服務了。</p>
                            <p>如果您有任何問題，歡迎隨時聯繫我們。</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                _logger.LogInformation($"開始發送郵件至: {toEmail}");
                _logger.LogInformation($"SMTP Server: {_emailSettings.SmtpServer}:{_emailSettings.SmtpPort}");
                _logger.LogInformation($"Sender: {_emailSettings.SenderEmail}");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // 啟用詳細的除錯日誌
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    _logger.LogInformation("正在連接到 SMTP 伺服器...");

                    // 連接到 SMTP 伺服器
                    await client.ConnectAsync(
                        _emailSettings.SmtpServer,
                        _emailSettings.SmtpPort,
                        _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None
                    );

                    _logger.LogInformation("SMTP 連接成功，開始驗證...");

                    // 驗證
                    await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                    _logger.LogInformation("驗證成功，開始發送郵件...");

                    // 發送郵件
                    await client.SendAsync(message);

                    _logger.LogInformation("郵件發送成功！");

                    // 斷開連接
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"郵件已成功發送至: {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"發送郵件時發生未預期的錯誤: {ex.Message}");
                _logger.LogError($"錯誤類型: {ex.GetType().Name}");
                _logger.LogError($"完整錯誤: {ex}");
                return false;
            }
        }
    }
}
