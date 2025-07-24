using BudgetTrackerCZ.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerCZ.Services
{
    public class FinancialTargetService
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;

        public FinancialTargetService(AppDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<FinancialTarget>> GetTargetsWithProgressAsync(int? year = null, int? month = null)
        {
            var targetDate = year.HasValue && month.HasValue 
                ? new DateTime(year.Value, month.Value, 1)
                : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var targets = await _context.FinancialTargets
                .Include(t => t.Category)
                .Where(t => t.Month.Year == targetDate.Year && t.Month.Month == targetDate.Month && t.IsActive)
                .ToListAsync();

            // Calculate actual amounts for each target
            foreach (var target in targets)
            {
                target.ActualAmount = await GetActualAmountForTargetAsync(target);
            }

            return targets;
        }

        public async Task<decimal> GetActualAmountForTargetAsync(FinancialTarget target)
        {
            var startOfMonth = new DateTime(target.Month.Year, target.Month.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var query = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth);

            // If target has a specific category, filter by that category
            if (target.CategoryId.HasValue && target.CategoryId > 0)
            {
                query = query.Where(t => t.CategoryId == target.CategoryId);
            }
            else
            {
                // If no specific category, get all transactions of the target type
                query = query.Where(t => t.Category != null && t.Category.Type == target.Type);
            }

            return await query.SumAsync(t => t.Amount);
        }

        public async Task<List<FinancialTarget>> GetCurrentMonthTargetsAsync()
        {
            return await GetTargetsWithProgressAsync(DateTime.Now.Year, DateTime.Now.Month);
        }

        public async Task<FinancialTargetSummary> GetMonthSummaryAsync(int year, int month)
        {
            var targets = await GetTargetsWithProgressAsync(year, month);
            
            var summary = new FinancialTargetSummary
            {
                Month = new DateTime(year, month, 1),
                TotalTargets = targets.Count,
                CompletedTargets = targets.Count(t => t.IsCompleted),
                OverTargets = targets.Count(t => t.IsOverTarget),
                OnTrackTargets = targets.Count(t => t.IsOnTrack && !t.IsCompleted && !t.IsOverTarget),
                TotalTargetAmount = targets.Sum(t => t.TargetAmount),
                TotalActualAmount = targets.Sum(t => t.ActualAmount),
                AverageProgress = targets.Any() ? targets.Average(t => t.PercentageAchieved) : 0
            };

            return summary;
        }

        public async Task CheckTargetNotificationsAsync()
        {
            var currentTargets = await GetCurrentMonthTargetsAsync();

            foreach (var target in currentTargets)
            {
                // Check if target is over budget
                if (target.IsOverTarget)
                {
                    var categoryTitle = target.Category != null ? target.Category.Title : "Unknown";
                    var existingNotification = await _context.Notifications
                        .Where(n => n.Type == "Warning" && 
                                   n.Title.Contains("Budget") && 
                                   n.Message.Contains(categoryTitle) &&
                                   n.CreatedDate >= DateTime.Now.AddDays(-7) &&
                                   !n.IsDismissed)
                        .FirstOrDefaultAsync();

                    if (existingNotification == null)
                    {
                        await _notificationService.CreateBudgetTargetNotificationAsync(target, target.ActualAmount);
                    }
                }
                // Check if target is significantly behind (more than 20% behind expected progress)
                else if (!target.IsOnTrack && target.Type == "Income")
                {
                    var daysInMonth = DateTime.DaysInMonth(target.Month.Year, target.Month.Month);
                    var daysElapsed = Math.Min(DateTime.Now.Day, daysInMonth);
                    var expectedProgress = (decimal)daysElapsed / daysInMonth * 100;
                    
                    if (target.PercentageAchieved < expectedProgress - 20)
                    {
                        var incomeTargetTitle = target.Category != null ? target.Category.Title : "Income Target";
                        var existingNotification = await _context.Notifications
                            .Where(n => n.Type == "Warning" && 
                                       n.Title.Contains("Behind") && 
                                       n.Message.Contains(incomeTargetTitle) &&
                                       n.CreatedDate >= DateTime.Now.AddDays(-7) &&
                                       !n.IsDismissed)
                            .FirstOrDefaultAsync();

                        if (existingNotification == null)
                        {
                            await _notificationService.CreateNotificationAsync(
                                title: "Income Target Behind Schedule",
                                message: $"Your {target.Category?.Title ?? "income target"} for {target.FormattedMonth} is {expectedProgress - target.PercentageAchieved:F1}% behind schedule.",
                                type: "Warning",
                                priority: "Medium",
                                actionUrl: "/FinancialTarget",
                                actionText: "View Targets"
                            );
                        }
                    }
                }
            }
        }

        public async Task<List<FinancialTarget>> GetHistoricalTargetsAsync(int monthsBack = 6)
        {
            var startDate = DateTime.Now.AddMonths(-monthsBack);
            
            var targets = await _context.FinancialTargets
                .Include(t => t.Category)
                .Where(t => t.Month >= startDate)
                .OrderBy(t => t.Month)
                .ThenBy(t => t.Category!.Title)
                .ToListAsync();

            // Calculate actual amounts for historical targets
            foreach (var target in targets)
            {
                target.ActualAmount = await GetActualAmountForTargetAsync(target);
            }

            return targets;
        }

        public async Task<Dictionary<string, decimal>> GetCategoryPerformanceAsync(int year, int month)
        {
            var targets = await GetTargetsWithProgressAsync(year, month);
            
            return targets
                .Where(t => t.Category != null)
                .GroupBy(t => t.Category!.Title)
                .ToDictionary(
                    g => g.Key, 
                    g => g.Average(t => t.PercentageAchieved)
                );
        }
    }

    public class FinancialTargetSummary
    {
        public DateTime Month { get; set; }
        public int TotalTargets { get; set; }
        public int CompletedTargets { get; set; }
        public int OverTargets { get; set; }
        public int OnTrackTargets { get; set; }
        public decimal TotalTargetAmount { get; set; }
        public decimal TotalActualAmount { get; set; }
        public decimal AverageProgress { get; set; }
        
        public decimal CompletionRate => TotalTargets > 0 ? (decimal)CompletedTargets / TotalTargets * 100 : 0;
        public decimal OverTargetRate => TotalTargets > 0 ? (decimal)OverTargets / TotalTargets * 100 : 0;
        public decimal VarianceAmount => TotalActualAmount - TotalTargetAmount;
    }
}