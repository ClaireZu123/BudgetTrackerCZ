using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetTrackerCZ.Models;

namespace BudgetTrackerCZ.Controllers
{
    public class ReminderController : Controller
    {
        private readonly AppDbContext _context;

        public ReminderController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Reminder
        public async Task<IActionResult> Index()
        {
            var reminders = await _context.Reminders
                .OrderBy(r => r.IsCompleted)
                .ThenBy(r => r.DueDate)
                .ToListAsync();
            
            ViewBag.OverdueCount = reminders.Count(r => r.IsOverdue);
            ViewBag.DueSoonCount = reminders.Count(r => r.IsDueSoon);
            ViewBag.CompletedCount = reminders.Count(r => r.IsCompleted);
            
            return View(reminders);
        }

        // GET: Reminder/AddOrEdit
        public IActionResult AddOrEdit(int id = 0)
        {
            PopulateReminderTypes();
            PopulatePriorities();
            PopulateRecurrenceTypes();
            
            if (id == 0)
                return View(new Reminder());
            else
                return View(_context.Reminders.Find(id));
        }

        // POST: Reminder/AddOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("ReminderId,Title,Description,DueDate,Type,Priority,IsRecurring,RecurrenceType")] Reminder reminder)
        {
            if (ModelState.IsValid)
            {
                if (reminder.ReminderId == 0)
                    _context.Add(reminder);
                else
                    _context.Update(reminder);
                    
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            PopulateReminderTypes();
            PopulatePriorities();
            PopulateRecurrenceTypes();
            return View(reminder);
        }

        // POST: Reminder/MarkComplete/5
        [HttpPost]
        public async Task<IActionResult> MarkComplete(int id)
        {
            var reminder = await _context.Reminders.FindAsync(id);
            if (reminder != null)
            {
                reminder.IsCompleted = true;
                reminder.CompletedDate = DateTime.Now;
                
                // Create next occurrence if recurring
                if (reminder.IsRecurring && !string.IsNullOrEmpty(reminder.RecurrenceType))
                {
                    var nextReminder = new Reminder
                    {
                        Title = reminder.Title,
                        Description = reminder.Description,
                        Type = reminder.Type,
                        Priority = reminder.Priority,
                        IsRecurring = reminder.IsRecurring,
                        RecurrenceType = reminder.RecurrenceType,
                        DueDate = CalculateNextDueDate(reminder.DueDate, reminder.RecurrenceType)
                    };
                    _context.Add(nextReminder);
                }
                
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Reminder/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reminder = await _context.Reminders.FindAsync(id);
            if (reminder != null)
            {
                _context.Reminders.Remove(reminder);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Active reminders for dashboard
        public async Task<IActionResult> GetActiveReminders()
        {
            var activeReminders = await _context.Reminders
                .Where(r => !r.IsCompleted && r.DueDate <= DateTime.Now.AddDays(7))
                .OrderBy(r => r.DueDate)
                .Take(5)
                .ToListAsync();
                
            return Json(activeReminders);
        }

        [NonAction]
        private void PopulateReminderTypes()
        {
            var types = new List<string> { "General", "ISA", "Bill Payment", "Tax", "Investment", "Insurance", "Savings" };
            ViewBag.ReminderTypes = types;
        }

        [NonAction]
        private void PopulatePriorities()
        {
            var priorities = new List<string> { "Low", "Medium", "High" };
            ViewBag.Priorities = priorities;
        }

        [NonAction]
        private void PopulateRecurrenceTypes()
        {
            var recurrenceTypes = new List<string> { "Monthly", "Quarterly", "Yearly" };
            ViewBag.RecurrenceTypes = recurrenceTypes;
        }

        [NonAction]
        private DateTime CalculateNextDueDate(DateTime currentDueDate, string recurrenceType)
        {
            return recurrenceType switch
            {
                "Monthly" => currentDueDate.AddMonths(1),
                "Quarterly" => currentDueDate.AddMonths(3),
                "Yearly" => currentDueDate.AddYears(1),
                _ => currentDueDate.AddMonths(1)
            };
        }
    }
}