namespace MonitoringWorker.Models;

/// <summary>
/// Notification severity levels
/// </summary>
public enum NotificationSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

/// <summary>
/// Base notification message
/// </summary>
public class NotificationMessage
{
    /// <summary>
    /// Notification subject/title
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Notification body/content
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Notification severity
    /// </summary>
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;

    /// <summary>
    /// Notification category (e.g., "health", "performance", "sla")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Source of the notification
    /// </summary>
    public string Source { get; set; } = "MonitoringWorker";

    /// <summary>
    /// Timestamp when notification was created
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Email notification configuration
/// </summary>
public class EmailNotification
{
    /// <summary>
    /// Email recipients
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// Email subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email body
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Whether the body is HTML
    /// </summary>
    public bool IsHtml { get; set; } = false;

    /// <summary>
    /// Email attachments
    /// </summary>
    public List<EmailAttachment>? Attachments { get; set; }
}

/// <summary>
/// Email attachment
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// Attachment filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Attachment content
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>
/// Slack notification configuration
/// </summary>
public class SlackNotification
{
    /// <summary>
    /// Slack channel
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Webhook URL
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Message title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Message text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Username to display
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Notification severity
    /// </summary>
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;

    /// <summary>
    /// Additional fields
    /// </summary>
    public Dictionary<string, string>? Fields { get; set; }
}

/// <summary>
/// Microsoft Teams notification configuration
/// </summary>
public class TeamsNotification
{
    /// <summary>
    /// Webhook URL
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Message title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Message text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Notification severity
    /// </summary>
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;

    /// <summary>
    /// Additional facts
    /// </summary>
    public Dictionary<string, string>? Facts { get; set; }
}

/// <summary>
/// Webhook notification configuration
/// </summary>
public class WebhookNotification
{
    /// <summary>
    /// Webhook URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Payload to send
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    /// HTTP headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();
}

/// <summary>
/// Notification channel configuration
/// </summary>
public class NotificationChannel
{
    /// <summary>
    /// Channel name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Channel type (email, slack, teams, webhook)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether the channel is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Target (email address, slack channel, etc.)
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Webhook URL for webhook-based channels
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Recipients list
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// Minimum severity to send notifications
    /// </summary>
    public NotificationSeverity? MinSeverity { get; set; }

    /// <summary>
    /// Only send during business hours
    /// </summary>
    public bool BusinessHoursOnly { get; set; } = false;

    /// <summary>
    /// Categories to include (empty = all)
    /// </summary>
    public List<string>? Categories { get; set; }

    /// <summary>
    /// Custom headers for webhook channels
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }
}

/// <summary>
/// Notification configuration
/// </summary>
public class NotificationConfiguration
{
    /// <summary>
    /// Whether notifications are enabled globally
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default sender information
    /// </summary>
    public NotificationSender Sender { get; set; } = new();

    /// <summary>
    /// Configured notification channels
    /// </summary>
    public List<NotificationChannel> Channels { get; set; } = new();

    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public NotificationRateLimit RateLimit { get; set; } = new();
}

/// <summary>
/// Notification sender configuration
/// </summary>
public class NotificationSender
{
    /// <summary>
    /// Sender name
    /// </summary>
    public string Name { get; set; } = "MonitoringWorker";

    /// <summary>
    /// Sender email address
    /// </summary>
    public string Email { get; set; } = "monitoring@company.com";

    /// <summary>
    /// Reply-to email address
    /// </summary>
    public string? ReplyTo { get; set; }
}

/// <summary>
/// Rate limiting configuration for notifications
/// </summary>
public class NotificationRateLimit
{
    /// <summary>
    /// Maximum notifications per minute
    /// </summary>
    public int MaxPerMinute { get; set; } = 10;

    /// <summary>
    /// Maximum notifications per hour
    /// </summary>
    public int MaxPerHour { get; set; } = 100;

    /// <summary>
    /// Burst allowance
    /// </summary>
    public int BurstAllowance { get; set; } = 5;
}
