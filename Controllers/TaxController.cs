using Microsoft.AspNetCore.Mvc;
using BudgetTrackerCZ.Models;
using BudgetTrackerCZ.Services;

namespace BudgetTrackerCZ.Controllers
{
    public class TaxController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TaxCalculationService _taxService;

        public TaxController(AppDbContext context)
        {
            _context = context;
            _taxService = new TaxCalculationService(context);
        }

        // GET: Tax
        public async Task<IActionResult> Index()
        {
            var currentTaxYear = TaxCalculationService.GetCurrentTaxYear();
            var warnings = await _taxService.GetActiveTaxWarningsAsync();
            var summary = await _taxService.GetTaxYearSummaryAsync(currentTaxYear);

            ViewBag.CurrentTaxYear = currentTaxYear;
            ViewBag.TaxYearStart = TaxCalculationService.GetTaxYearStart(currentTaxYear);
            ViewBag.TaxYearEnd = TaxCalculationService.GetTaxYearEnd(currentTaxYear);
            ViewBag.Warnings = warnings;
            ViewBag.Summary = summary;

            // Calculate days remaining in tax year
            var today = DateTime.Today;
            var taxYearEnd = TaxCalculationService.GetTaxYearEnd(currentTaxYear);
            var daysRemaining = (taxYearEnd - today).Days;
            ViewBag.DaysRemaining = Math.Max(0, daysRemaining);

            return View();
        }

        // GET: Tax/History
        public async Task<IActionResult> History()
        {
            var currentTaxYear = TaxCalculationService.GetCurrentTaxYear();
            var taxYears = new List<int>();
            
            // Show last 3 tax years
            for (int i = 0; i < 3; i++)
            {
                taxYears.Add(currentTaxYear - i);
            }

            var historicalData = new Dictionary<int, Dictionary<string, decimal>>();
            
            foreach (var year in taxYears)
            {
                historicalData[year] = await _taxService.GetTaxYearSummaryAsync(year);
            }

            ViewBag.TaxYears = taxYears;
            ViewBag.HistoricalData = historicalData;

            return View();
        }

        // GET: Tax/Warnings (API endpoint)
        [HttpGet]
        public async Task<IActionResult> GetWarnings()
        {
            var warnings = await _taxService.GetActiveTaxWarningsAsync();
            return Json(warnings);
        }

        // GET: Tax/Summary/{taxYear}
        public async Task<IActionResult> GetTaxYearSummary(int taxYear)
        {
            var summary = await _taxService.GetTaxYearSummaryAsync(taxYear);
            var warnings = await _taxService.GetActiveTaxWarningsAsync();

            return Json(new
            {
                TaxYear = taxYear,
                TaxYearStart = TaxCalculationService.GetTaxYearStart(taxYear),
                TaxYearEnd = TaxCalculationService.GetTaxYearEnd(taxYear),
                Summary = summary,
                Warnings = warnings.Where(w => w.TaxYear == taxYear).ToList()
            });
        }
    }
}