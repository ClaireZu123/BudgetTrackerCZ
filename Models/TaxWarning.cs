using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTrackerCZ.Models
{
    public class TaxWarning
    {
        [Key]
        public int TaxWarningId { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(50)")]
        public string Type { get; set; } = "Interest"; // Interest, Capital Gains, Income, etc.

        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public string Message { get; set; }

        [Required]
        public decimal Threshold { get; set; }

        [Required]
        public decimal CurrentAmount { get; set; }

        [Required]
        public int TaxYear { get; set; } // e.g., 2024 for 2024-25 tax year

        [Required]
        [Column(TypeName = "nvarchar(20)")]
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [NotMapped]
        public decimal RemainingAllowance
        {
            get
            {
                return Math.Max(0, Threshold - CurrentAmount);
            }
        }

        [NotMapped]
        public decimal PercentageUsed
        {
            get
            {
                return Threshold > 0 ? Math.Min(100, (CurrentAmount / Threshold) * 100) : 0;
            }
        }

        [NotMapped]
        public bool IsNearThreshold
        {
            get
            {
                return PercentageUsed >= 75;
            }
        }

        [NotMapped]
        public bool HasExceededThreshold
        {
            get
            {
                return CurrentAmount >= Threshold;
            }
        }

        [NotMapped]
        public string SeverityClass
        {
            get
            {
                return Severity switch
                {
                    "Critical" => "text-danger",
                    "High" => "text-warning",
                    "Medium" => "text-info",
                    "Low" => "text-secondary",
                    _ => "text-muted"
                };
            }
        }

        [NotMapped]
        public string SeverityBadgeClass
        {
            get
            {
                return Severity switch
                {
                    "Critical" => "bg-danger",
                    "High" => "bg-warning",
                    "Medium" => "bg-info",
                    "Low" => "bg-secondary",
                    _ => "bg-muted"
                };
            }
        }
    }
}