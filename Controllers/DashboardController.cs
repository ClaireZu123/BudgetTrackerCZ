﻿using Microsoft.AspNetCore.Mvc;
using BudgetTrackerCZ.Models;
using BudgetTrackerCZ.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BudgetTrackerCZ.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TaxCalculationService _taxService;
        private readonly NotificationService _notificationService;

        public DashboardController(AppDbContext context, TaxCalculationService taxService, NotificationService notificationService)
        {
            _context = context;
            _taxService = taxService;
            _notificationService = notificationService;
        }
        public async Task<ActionResult> Index()
        {
            //Last 7 Days
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            //Total Income
            int TotalIncome = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C0");

            //Total Expense
            int TotalExpense = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("C0");

            //Balance
            int Balance = TotalIncome - TotalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-GB");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            //Doughnut Chart - Expense By Category
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                })
                .OrderByDescending(l => l.amount)
                .ToList();


            //Income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            //Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            //Combine Income & Expense
            string[] Last7Days = Enumerable.Range(0, 7)
                .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in Last7Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };

            //Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i => i.Category)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            //Active Reminders
            ViewBag.ActiveReminders = await _context.Reminders
                .Where(r => !r.IsCompleted && r.DueDate <= DateTime.Now.AddDays(7))
                .OrderBy(r => r.DueDate)
                .Take(5)
                .ToListAsync();

            //Tax Warnings
            ViewBag.TaxWarnings = await _taxService.GetActiveTaxWarningsAsync();

            // Generate system notifications
            await _notificationService.GenerateSystemNotificationsAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;

    }
}
