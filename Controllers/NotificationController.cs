using Microsoft.AspNetCore.Mvc;
using BudgetTrackerCZ.Models;
using BudgetTrackerCZ.Services;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerCZ.Controllers
{
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;

        public NotificationController(AppDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: Notification
        public async Task<IActionResult> Index()
        {
            var notifications = await _context.Notifications
                .Where(n => !n.IsDismissed)
                .OrderByDescending(n => n.CreatedDate)
                .Take(50)
                .ToListAsync();

            var unreadCount = await _notificationService.GetUnreadCountAsync();
            
            ViewBag.UnreadCount = unreadCount;
            ViewBag.TotalCount = notifications.Count;

            return View(notifications);
        }

        // GET: Notification/Unread
        public async Task<IActionResult> Unread()
        {
            var notifications = await _notificationService.GetUnreadNotificationsAsync();
            return View("Index", notifications);
        }

        // POST: Notification/MarkAsRead/{id}
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = true });
        }

        // POST: Notification/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync();
            return Json(new { success = true });
        }

        // POST: Notification/Dismiss/{id}
        [HttpPost]
        public async Task<IActionResult> Dismiss(int id)
        {
            await _notificationService.DismissNotificationAsync(id);
            return Json(new { success = true });
        }

        // GET: API endpoint for notification count
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _notificationService.GetUnreadCountAsync();
            return Json(new { count = count });
        }

        // GET: API endpoint for recent notifications
        [HttpGet]
        public async Task<IActionResult> GetRecentNotifications(int limit = 5)
        {
            var notifications = await _notificationService.GetActiveNotificationsAsync(limit);
            return Json(notifications.Select(n => new
            {
                n.NotificationId,
                n.Title,
                n.Message,
                n.Type,
                n.Priority,
                n.IsRead,
                n.TypeIcon,
                n.TypeColor,
                n.FormattedCreatedDate,
                n.ActionUrl,
                n.ActionText,
                n.PriorityBadgeClass
            }));
        }

        // POST: Generate system notifications (can be called by scheduled task)
        [HttpPost]
        public async Task<IActionResult> GenerateSystemNotifications()
        {
            await _notificationService.GenerateSystemNotificationsAsync();
            return Json(new { success = true, message = "System notifications generated" });
        }

        // POST: Cleanup old notifications
        [HttpPost]
        public async Task<IActionResult> CleanupOldNotifications(int daysToKeep = 30)
        {
            await _notificationService.CleanupOldNotificationsAsync(daysToKeep);
            return Json(new { success = true, message = $"Cleaned up notifications older than {daysToKeep} days" });
        }
    }
}