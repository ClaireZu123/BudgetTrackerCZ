using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTrackerCZ.Models
{
    public class Reminder
    {
        [Key]
        public int ReminderId { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public string Title { get; set; }

        [Column(TypeName = "nvarchar(500)")]
        public string? Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(20)")]
        public string Type { get; set; } = "General"; // General, ISA, Bill, Tax, etc.

        [Column(TypeName = "nvarchar(20)")]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High

        public bool IsCompleted { get; set; } = false;

        public bool IsRecurring { get; set; } = false;

        [Column(TypeName = "nvarchar(20)")]
        public string? RecurrenceType { get; set; } // Monthly, Yearly, etc.

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? CompletedDate { get; set; }

        [NotMapped]
        public bool IsOverdue
        {
            get
            {
                return !IsCompleted && DueDate < DateTime.Now;
            }
        }

        [NotMapped]
        public bool IsDueSoon
        {
            get
            {
                return !IsCompleted && DueDate <= DateTime.Now.AddDays(7) && DueDate >= DateTime.Now;
            }
        }

        [NotMapped]
        public string FormattedDueDate
        {
            get
            {
                return DueDate.ToString("dd MMM yyyy");
            }
        }

        [NotMapped]
        public string StatusClass
        {
            get
            {
                if (IsCompleted) return "text-success";
                if (IsOverdue) return "text-danger";
                if (IsDueSoon) return "text-warning";
                return "text-muted";
            }
        }
    }
}