using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MonitoringWorker.Models;
using MonitoringWorker.Services;

namespace MonitoringWorker.Services;

/// <summary>
/// Service for sending notifications through multiple channels
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send notification to all configured channels
    /// </summary>
    Task SendNotificationAsync(NotificationMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send notification to specific channel
    /// </summary>
    Task SendNotificationAsync(NotificationMessage message, NotificationChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send email notification
    /// </summary>
    Task SendEmailAsync(EmailNotification email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send Slack notification
    /// </summary>
    Task SendSlackAsync(SlackNotification slack, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send Teams notification
    /// </summary>
    Task SendTeamsAsync(TeamsNotification teams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send webhook notification
    /// </summary>
    Task SendWebhookAsync(WebhookNotification webhook, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test notification channel
    /// </summary>
    Task<bool> TestChannelAsync(NotificationChannel channel, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of notification service
/// </summary>
public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationService> _logger;
    private readonly IFeatureToggleService _featureToggle;
    private readonly NotificationConfiguration _config;

    public NotificationService(
        HttpClient httpClient,
        ILogger<NotificationService> logger,
        IFeatureToggleService featureToggle,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _featureToggle = featureToggle;
        _config = configuration.GetSection("Notifications").Get<NotificationConfiguration>() ?? new();
    }

    public async Task SendNotificationAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (!_featureToggle.IsEnabled("notifications"))
        {
            _logger.LogDebug("Notifications are disabled, skipping message: {Subject}", message.Subject);
            return;
        }

        var tasks = new List<Task>();

        // Send to all enabled channels based on severity
        foreach (var channel in _config.Channels.Where(c => c.Enabled && ShouldSendToChannel(message, c)))
        {
            tasks.Add(SendNotificationAsync(message, channel, cancellationToken));
        }

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
            _logger.LogInformation("Sent notification '{Subject}' to {ChannelCount} channels", 
                message.Subject, tasks.Count);
        }
        else
        {
            _logger.LogWarning("No enabled notification channels found for message: {Subject}", message.Subject);
        }
    }

    public async Task SendNotificationAsync(NotificationMessage message, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (channel.Type.ToLowerInvariant())
            {
                case "email":
                    await SendEmailAsync(CreateEmailNotification(message, channel), cancellationToken);
                    break;
                case "slack":
                    await SendSlackAsync(CreateSlackNotification(message, channel), cancellationToken);
                    break;
                case "teams":
                    await SendTeamsAsync(CreateTeamsNotification(message, channel), cancellationToken);
                    break;
                case "webhook":
                    await SendWebhookAsync(CreateWebhookNotification(message, channel), cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Unknown notification channel type: {Type}", channel.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to {ChannelType} channel '{ChannelName}'", 
                channel.Type, channel.Name);
            throw;
        }
    }

    public async Task SendEmailAsync(EmailNotification email, CancellationToken cancellationToken = default)
    {
        if (!_featureToggle.IsEnabled("email-notifications"))
        {
            _logger.LogDebug("Email notifications are disabled");
            return;
        }

        // In a real implementation, you would use an email service like SendGrid, AWS SES, etc.
        // For now, we'll log the email content
        _logger.LogInformation("Email notification sent to {Recipients}: {Subject}", 
            string.Join(", ", email.Recipients), email.Subject);

        // Simulate email sending
        await Task.Delay(100, cancellationToken);
    }

    public async Task SendSlackAsync(SlackNotification slack, CancellationToken cancellationToken = default)
    {
        if (!_featureToggle.IsEnabled("slack-notifications"))
        {
            _logger.LogDebug("Slack notifications are disabled");
            return;
        }

        var payload = new
        {
            channel = slack.Channel,
            text = slack.Text,
            username = slack.Username ?? "MonitoringWorker",
            icon_emoji = GetSeverityEmoji(slack.Severity),
            attachments = new[]
            {
                new
                {
                    color = GetSeverityColor(slack.Severity),
                    title = slack.Title,
                    text = slack.Text,
                    fields = slack.Fields?.Select(f => new { title = f.Key, value = f.Value, @short = true }),
                    ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(slack.WebhookUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Slack notification sent to {Channel}: {Title}", slack.Channel, slack.Title);
    }

    public async Task SendTeamsAsync(TeamsNotification teams, CancellationToken cancellationToken = default)
    {
        if (!_featureToggle.IsEnabled("teams-notifications"))
        {
            _logger.LogDebug("Teams notifications are disabled");
            return;
        }

        var payload = new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.3",
                        body = new object[]
                        {
                            new
                            {
                                type = "TextBlock",
                                text = teams.Title,
                                weight = "Bolder",
                                size = "Medium",
                                color = GetTeamsSeverityColor(teams.Severity)
                            },
                            new
                            {
                                type = "TextBlock",
                                text = teams.Text,
                                wrap = true
                            },
                            new
                            {
                                type = "FactSet",
                                facts = teams.Facts?.Select(f => new { title = f.Key, value = f.Value })
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(teams.WebhookUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Teams notification sent: {Title}", teams.Title);
    }

    public async Task SendWebhookAsync(WebhookNotification webhook, CancellationToken cancellationToken = default)
    {
        if (!_featureToggle.IsEnabled("webhook-notifications"))
        {
            _logger.LogDebug("Webhook notifications are disabled");
            return;
        }

        var content = new StringContent(webhook.Payload, Encoding.UTF8, webhook.ContentType);

        // Add custom headers
        foreach (var header in webhook.Headers)
        {
            content.Headers.Add(header.Key, header.Value);
        }

        var response = await _httpClient.PostAsync(webhook.Url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Webhook notification sent to {Url}", webhook.Url);
    }

    public async Task<bool> TestChannelAsync(NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        try
        {
            var testMessage = new NotificationMessage
            {
                Subject = "Test Notification",
                Body = "This is a test notification from MonitoringWorker",
                Severity = NotificationSeverity.Info,
                Timestamp = DateTimeOffset.UtcNow,
                Source = "MonitoringWorker.Test"
            };

            await SendNotificationAsync(testMessage, channel, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test notification channel '{ChannelName}'", channel.Name);
            return false;
        }
    }

    private bool ShouldSendToChannel(NotificationMessage message, NotificationChannel channel)
    {
        // Check severity filter
        if (channel.MinSeverity.HasValue && message.Severity < channel.MinSeverity.Value)
        {
            return false;
        }

        // Check time-based filters (e.g., business hours only)
        if (channel.BusinessHoursOnly && !IsBusinessHours())
        {
            return false;
        }

        // Check category filters
        if (channel.Categories?.Any() == true && !channel.Categories.Contains(message.Category))
        {
            return false;
        }

        return true;
    }

    private bool IsBusinessHours()
    {
        var now = DateTime.Now;
        return now.DayOfWeek >= DayOfWeek.Monday && 
               now.DayOfWeek <= DayOfWeek.Friday && 
               now.Hour >= 9 && 
               now.Hour < 17;
    }

    private EmailNotification CreateEmailNotification(NotificationMessage message, NotificationChannel channel)
    {
        return new EmailNotification
        {
            Recipients = channel.Recipients,
            Subject = $"[{message.Severity.ToString().ToUpper()}] {message.Subject}",
            Body = FormatEmailBody(message),
            IsHtml = true
        };
    }

    private SlackNotification CreateSlackNotification(NotificationMessage message, NotificationChannel channel)
    {
        return new SlackNotification
        {
            Channel = channel.Target,
            WebhookUrl = channel.WebhookUrl!,
            Title = message.Subject,
            Text = message.Body,
            Severity = message.Severity,
            Fields = message.Metadata
        };
    }

    private TeamsNotification CreateTeamsNotification(NotificationMessage message, NotificationChannel channel)
    {
        return new TeamsNotification
        {
            WebhookUrl = channel.WebhookUrl!,
            Title = message.Subject,
            Text = message.Body,
            Severity = message.Severity,
            Facts = message.Metadata
        };
    }

    private WebhookNotification CreateWebhookNotification(NotificationMessage message, NotificationChannel channel)
    {
        var payload = JsonSerializer.Serialize(new
        {
            message.Subject,
            message.Body,
            message.Severity,
            message.Category,
            message.Source,
            message.Timestamp,
            message.Metadata
        });

        return new WebhookNotification
        {
            Url = channel.WebhookUrl!,
            Payload = payload,
            ContentType = "application/json",
            Headers = channel.Headers ?? new Dictionary<string, string>()
        };
    }

    private string FormatEmailBody(NotificationMessage message)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<h2>{message.Subject}</h2>");
        sb.AppendLine($"<p><strong>Severity:</strong> {message.Severity}</p>");
        sb.AppendLine($"<p><strong>Source:</strong> {message.Source}</p>");
        sb.AppendLine($"<p><strong>Time:</strong> {message.Timestamp:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine($"<p><strong>Description:</strong></p>");
        sb.AppendLine($"<p>{message.Body}</p>");

        if (message.Metadata?.Any() == true)
        {
            sb.AppendLine("<h3>Additional Information:</h3>");
            sb.AppendLine("<ul>");
            foreach (var item in message.Metadata)
            {
                sb.AppendLine($"<li><strong>{item.Key}:</strong> {item.Value}</li>");
            }
            sb.AppendLine("</ul>");
        }

        return sb.ToString();
    }

    private string GetSeverityEmoji(NotificationSeverity severity)
    {
        return severity switch
        {
            NotificationSeverity.Critical => ":rotating_light:",
            NotificationSeverity.Warning => ":warning:",
            NotificationSeverity.Info => ":information_source:",
            _ => ":grey_question:"
        };
    }

    private string GetSeverityColor(NotificationSeverity severity)
    {
        return severity switch
        {
            NotificationSeverity.Critical => "danger",
            NotificationSeverity.Warning => "warning",
            NotificationSeverity.Info => "good",
            _ => "#808080"
        };
    }

    private string GetTeamsSeverityColor(NotificationSeverity severity)
    {
        return severity switch
        {
            NotificationSeverity.Critical => "Attention",
            NotificationSeverity.Warning => "Warning",
            NotificationSeverity.Info => "Good",
            _ => "Default"
        };
    }
}
