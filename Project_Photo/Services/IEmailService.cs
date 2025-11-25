using System.Threading.Tasks;

namespace Project_Photo.Services
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetUrl, string verificationCode);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}