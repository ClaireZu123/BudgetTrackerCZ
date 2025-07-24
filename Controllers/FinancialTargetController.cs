using BudgetTrackerCZ.Models;
using BudgetTrackerCZ.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerCZ.Controllers
{
    public class FinancialTargetController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FinancialTargetService _targetService;

        public FinancialTargetController(AppDbContext context, FinancialTargetService targetService)
        {
            _context = context;
            _targetService = targetService;
        }

        public async Task<IActionResult> Index(int? year, int? month)
        {
            var targetYear = year ?? DateTime.Now.Year;
            var targetMonth = month ?? DateTime.Now.Month;

            var targets = await _targetService.GetTargetsWithProgressAsync(targetYear, targetMonth);
            var summary = await _targetService.GetMonthSummaryAsync(targetYear, targetMonth);

            ViewBag.CurrentYear = targetYear;
            ViewBag.CurrentMonth = targetMonth;
            ViewBag.Summary = summary;
            ViewBag.MonthName = new DateTime(targetYear, targetMonth, 1).ToString("MMMM yyyy");

            return View(targets);
        }

        public IActionResult AddOrEdit(int id = 0)
        {
            PopulateCategories();

            if (id == 0)
                return View(new FinancialTarget());
            else
                return View(_context.FinancialTargets.Find(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("Id,Month,Type,TargetAmount,CategoryId,Description,IsActive")] FinancialTarget target)
        {
            if (ModelState.IsValid)
            {
                if (target.Id == 0)
                    _context.Add(target);
                else
                    _context.Update(target);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateCategories();
            return View(target);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var target = await _context.FinancialTargets.FindAsync(id);
            if (target != null)
                _context.FinancialTargets.Remove(target);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: FinancialTarget/Analytics
        public async Task<IActionResult> Analytics()
        {
            var currentTargets = await _targetService.GetCurrentMonthTargetsAsync();
            var historicalTargets = await _targetService.GetHistoricalTargetsAsync(6);
            var categoryPerformance = await _targetService.GetCategoryPerformanceAsync(DateTime.Now.Year, DateTime.Now.Month);

            ViewBag.CurrentTargets = currentTargets;
            ViewBag.HistoricalTargets = historicalTargets;
            ViewBag.CategoryPerformance = categoryPerformance;

            return View();
        }

        // GET: FinancialTarget/Progress/{id}
        public async Task<IActionResult> Progress(int id)
        {
            var target = await _context.FinancialTargets
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (target == null)
                return NotFound();

            target.ActualAmount = await _targetService.GetActualAmountForTargetAsync(target);

            // Get daily progress for the month
            var dailyProgress = await GetDailyProgressAsync(target);
            ViewBag.DailyProgress = dailyProgress;

            return View(target);
        }

        // API endpoint for target summary
        [HttpGet]
        public async Task<IActionResult> GetTargetSummary(int year, int month)
        {
            var summary = await _targetService.GetMonthSummaryAsync(year, month);
            return Json(summary);
        }

        // POST: FinancialTarget/ToggleActive/{id}
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var target = await _context.FinancialTargets.FindAsync(id);
            if (target != null)
            {
                target.IsActive = !target.IsActive;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, isActive = target?.IsActive ?? false });
        }

        private async Task<List<DailyProgress>> GetDailyProgressAsync(FinancialTarget target)
        {
            var startOfMonth = new DateTime(target.Month.Year, target.Month.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            var today = DateTime.Now.Date;

            var dailyProgress = new List<DailyProgress>();

            // Get transactions for the month
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth)
                .Where(t => target.CategoryId.HasValue ? 
                           t.CategoryId == target.CategoryId : 
                           t.Category != null && t.Category.Type == target.Type)
                .OrderBy(t => t.Date)
                .ToListAsync();

            decimal runningTotal = 0;
            var currentDate = startOfMonth;

            while (currentDate <= (endOfMonth < today ? endOfMonth : today))
            {
                var dayTransactions = transactions.Where(t => t.Date.Date == currentDate).Sum(t => t.Amount);
                runningTotal += dayTransactions;

                dailyProgress.Add(new DailyProgress
                {
                    Date = currentDate,
                    DailyAmount = dayTransactions,
                    CumulativeAmount = runningTotal,
                    TargetAmount = target.TargetAmount
                });

                currentDate = currentDate.AddDays(1);
            }

            return dailyProgress;
        }

        private void PopulateCategories()
        {
            var categoryList = _context.Categories.ToList();
            categoryList.Insert(0, new Category { CategoryId = 0, Title = "Choose a Category" });
            ViewBag.Categories = categoryList;
        }
    }

    public class DailyProgress
    {
        public DateTime Date { get; set; }
        public decimal DailyAmount { get; set; }
        public decimal CumulativeAmount { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal PercentageAchieved => TargetAmount > 0 ? (CumulativeAmount / TargetAmount) * 100 : 0;
    }
}
