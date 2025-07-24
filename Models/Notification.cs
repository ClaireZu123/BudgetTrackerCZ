using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTrackerCZ.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public string Title { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(500)")]
        public string Message { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(50)")]
        public string Type { get; set; } = "Info"; // Info, Warning, Error, Success, Reminder, Tax

        [Required]
        [Column(TypeName = "nvarchar(20)")]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical

        public bool IsRead { get; set; } = false;

        public bool IsDismissed { get; set; } = false;

        [Column(TypeName = "nvarchar(200)")]
        public string? ActionUrl { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? ActionText { get; set; }

        // Reference IDs for related entities
        public int? ReminderId { get; set; }
        public int? TaxWarningId { get; set; }
        public int? TransactionId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ReadDate { get; set; }

        public DateTime? DismissedDate { get; set; }

        [NotMapped]
        public bool IsNew
        {
            get
            {
                return CreatedDate >= DateTime.Now.AddHours(-24) && !IsRead;
            }
        }

        [NotMapped]
        public string TypeIcon
        {
            get
            {
                return Type switch
                {
                    "Info" => "fa-info-circle",
                    "Warning" => "fa-exclamation-triangle",
                    "Error" => "fa-times-circle",
                    "Success" => "fa-check-circle",
                    "Reminder" => "fa-bell",
                    "Tax" => "fa-file-invoice-dollar",
                    _ => "fa-info-circle"
                };
            }
        }

        [NotMapped]
        public string TypeColor
        {
            get
            {
                return Type switch
                {
                    "Info" => "text-info",
                    "Warning" => "text-warning",
                    "Error" => "text-danger",
                    "Success" => "text-success",
                    "Reminder" => "text-primary",
                    "Tax" => "text-secondary",
                    _ => "text-muted"
                };
            }
        }

        [NotMapped]
        public string PriorityBadgeClass
        {
            get
            {
                return Priority switch
                {
                    "Critical" => "bg-danger",
                    "High" => "bg-warning text-dark",
                    "Medium" => "bg-info",
                    "Low" => "bg-secondary",
                    _ => "bg-light text-dark"
                };
            }
        }

        [NotMapped]
        public string FormattedCreatedDate
        {
            get
            {
                var timeSpan = DateTime.Now - CreatedDate;
                
                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                else if (timeSpan.TotalHours < 1)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                else if (timeSpan.TotalDays < 1)
                    return $"{(int)timeSpan.TotalHours}h ago";
                else if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d ago";
                else
                    return CreatedDate.ToString("dd MMM yyyy");
            }
        }
    }
}