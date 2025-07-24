using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTrackerCZ.Models
{
    public class FinancialTarget
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM}")]
        public DateTime Month { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        [Required]
        [Column(TypeName = "nvarchar(10)")]
        public string Type { get; set; } = "Expense"; // or "Income"

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Target must be greater than 0.")]
        public decimal TargetAmount { get; set; }

        [Column(TypeName = "nvarchar(500)")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? CompletedDate { get; set; }

        // Progress tracking properties (not mapped to database)
        [NotMapped]
        public decimal ActualAmount { get; set; }

        [NotMapped]
        public decimal VarianceAmount 
        { 
            get 
            { 
                return Type == "Expense" ? TargetAmount - ActualAmount : ActualAmount - TargetAmount; 
            } 
        }

        [NotMapped]
        public decimal PercentageAchieved 
        { 
            get 
            { 
                return TargetAmount > 0 ? (ActualAmount / TargetAmount) * 100 : 0; 
            } 
        }

        [NotMapped]
        public bool IsOnTrack 
        { 
            get 
            {
                var daysInMonth = DateTime.DaysInMonth(Month.Year, Month.Month);
                var daysElapsed = Math.Min(DateTime.Now.Day, daysInMonth);
                var expectedProgress = (decimal)daysElapsed / daysInMonth * 100;
                
                return Type == "Expense" ? PercentageAchieved <= expectedProgress + 10 
                                        : PercentageAchieved >= expectedProgress - 10;
            } 
        }

        [NotMapped]
        public bool IsOverTarget 
        { 
            get 
            { 
                return Type == "Expense" ? ActualAmount > TargetAmount : false; 
            } 
        }

        [NotMapped]
        public bool IsCompleted 
        { 
            get 
            { 
                return Type == "Income" ? ActualAmount >= TargetAmount : false; 
            } 
        }

        [NotMapped]
        public string StatusClass 
        { 
            get 
            {
                if (IsOverTarget) return "text-danger";
                if (IsCompleted) return "text-success";
                if (!IsOnTrack) return "text-warning";
                return "text-info";
            } 
        }

        [NotMapped]
        public string StatusText 
        { 
            get 
            {
                if (IsOverTarget) return "Over Budget";
                if (IsCompleted) return "Target Achieved";
                if (!IsOnTrack) return "Behind Target";
                return "On Track";
            } 
        }

        [NotMapped]
        public string ProgressBarClass 
        { 
            get 
            {
                if (IsOverTarget) return "bg-danger";
                if (IsCompleted) return "bg-success";
                if (!IsOnTrack) return "bg-warning";
                return "bg-info";
            } 
        }

        [NotMapped]
        public string FormattedMonth 
        { 
            get 
            { 
                return Month.ToString("MMMM yyyy"); 
            } 
        }
    }
}
