using System.Text;
using System.Text.Json;
using DotNetEnv;

namespace backend.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string name, string token);
}

public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;

    public EmailService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendVerificationEmailAsync(string email, string name, string token)
    {
        var apiKey = Env.GetString("RESEND_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("Warning: RESEND_API_KEY is not set. Skipping email send.");
            return;
        }

        var verificationLink = $"https://usermanagement.anzdevelopers.com/verify?token={token}";

        var htmlContent = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 8px;'>
                <h2 style='color: #2563eb; text-align: center;'>Welcome to The App, {name}!</h2>
                <p style='color: #374151; font-size: 16px;'>Thanks for signing up. Please verify your email address to get full access to your account.</p>
                <div style='text-align: center; margin-top: 30px; margin-bottom: 30px;'>
                    <a href='{verificationLink}' style='background-color: #2563eb; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Verify Email</a>
                </div>
                <p style='color: #6b7280; font-size: 14px; text-align: center;'>If the button doesn't work, copy and paste this link into your browser:<br/>
                <a href='{verificationLink}' style='color: #2563eb;'>{verificationLink}</a></p>
            </div>
        ";

        var payload = new
        {
            from = "User Management<onboarding@anzdevelopers.com>",
            to = new[] { email },
            subject = "Verify your email address",
            html = htmlContent
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error sending email: {error}");
        }
    }
}
