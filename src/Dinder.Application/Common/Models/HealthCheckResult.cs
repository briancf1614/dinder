namespace Dinder.Application.Common.Models
{

    public class HealthCheckResult
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
