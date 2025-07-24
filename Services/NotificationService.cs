using BudgetTrackerCZ.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerCZ.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> CreateNotificationAsync(string title, string message, string type = "Info", string priority = "Medium", string? actionUrl = null, string? actionText = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Priority = priority,
                ActionUrl = actionUrl,
                ActionText = actionText,
                CreatedDate = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            
            return notification;
        }

        public async Task CreateReminderNotificationAsync(Reminder reminder)
        {
            var priority = reminder.IsOverdue ? "Critical" : reminder.IsDueSoon ? "High" : "Medium";
            var message = reminder.IsOverdue 
                ? $"Reminder '{reminder.Title}' is overdue (due: {reminder.FormattedDueDate})"
                : $"Reminder '{reminder.Title}' is due soon ({reminder.FormattedDueDate})";

            await CreateNotificationAsync(
                title: "Reminder Alert",
                message: message,
                type: "Reminder",
                priority: priority,
                actionUrl: $"/Reminder/AddOrEdit/{reminder.ReminderId}",
                actionText: "View Reminder"
            );
        }

        public async Task CreateTaxWarningNotificationAsync(TaxWarning warning)
        {
            var priority = warning.HasExceededThreshold ? "Critical" : warning.IsNearThreshold ? "High" : "Medium";
            
            await CreateNotificationAsync(
                title: $"Tax {warning.Type} Warning",
                message: warning.Message,
                type: "Tax",
                priority: priority,
                actionUrl: "/Tax",
                actionText: "View Tax Details"
            );
        }

        public async Task CreateBudgetTargetNotificationAsync(FinancialTarget target, decimal actualAmount)
        {
            var percentageUsed = target.TargetAmount > 0 ? (actualAmount / target.TargetAmount) * 100 : 0;
            
            var priority = percentageUsed >= 100 ? "Critical" : percentageUsed >= 80 ? "High" : "Medium";
            var message = percentageUsed >= 100 
                ? $"Budget target for {target.Category?.Title ?? "Unknown"} has been exceeded by £{actualAmount - target.TargetAmount:F2}"
                : $"You've used {percentageUsed:F1}% of your budget target for {target.Category?.Title ?? "Unknown"}";

            await CreateNotificationAsync(
                title: "Budget Target Alert",
                message: message,
                type: "Warning",
                priority: priority,
                actionUrl: "/FinancialTarget",
                actionText: "View Targets"
            );
        }

        public async Task CreateTransactionNotificationAsync(string title, string message, int? transactionId = null)
        {
            await CreateNotificationAsync(
                title: title,
                message: message,
                type: "Info",
                priority: "Low",
                actionUrl: transactionId.HasValue ? $"/Transaction/AddOrEdit/{transactionId}" : "/Transaction",
                actionText: "View Transaction"
            );
        }

        public async Task<List<Notification>> GetActiveNotificationsAsync(int limit = 10)
        {
            return await _context.Notifications
                .Where(n => !n.IsDismissed)
                .OrderByDescending(n => n.CreatedDate)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync()
        {
            return await _context.Notifications
                .Where(n => !n.IsRead && !n.IsDismissed)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync()
        {
            return await _context.Notifications
                .Where(n => !n.IsRead && !n.IsDismissed)
                .CountAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync()
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => !n.IsRead && !n.IsDismissed)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DismissNotificationAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsDismissed = true;
                notification.DismissedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task GenerateSystemNotificationsAsync()
        {
            // Check for overdue reminders
            var overdueReminders = await _context.Reminders
                .Where(r => !r.IsCompleted && r.DueDate < DateTime.Now)
                .ToListAsync();

            foreach (var reminder in overdueReminders)
            {
                // Check if we already have a notification for this reminder
                var existingNotification = await _context.Notifications
                    .Where(n => n.ReminderId == reminder.ReminderId && !n.IsDismissed)
                    .FirstOrDefaultAsync();

                if (existingNotification == null)
                {
                    await CreateReminderNotificationAsync(reminder);
                }
            }

            // Check for tax warnings
            var taxService = new TaxCalculationService(_context);
            var taxWarnings = await taxService.GetActiveTaxWarningsAsync();

            foreach (var warning in taxWarnings)
            {
                // Check if we already have a notification for this type of warning
                var existingNotification = await _context.Notifications
                    .Where(n => n.Type == "Tax" && 
                               n.Title.Contains(warning.Type) && 
                               !n.IsDismissed &&
                               n.CreatedDate >= DateTime.Now.AddDays(-7)) 
                    .FirstOrDefaultAsync();

                if (existingNotification == null)
                {
                    await CreateTaxWarningNotificationAsync(warning);
                }
            }
        }

        public async Task CleanupOldNotificationsAsync(int daysToKeep = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            
            var oldNotifications = await _context.Notifications
                .Where(n => n.CreatedDate < cutoffDate && (n.IsRead || n.IsDismissed))
                .ToListAsync();

            _context.Notifications.RemoveRange(oldNotifications);
            await _context.SaveChangesAsync();
        }
    }
}