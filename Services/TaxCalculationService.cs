using BudgetTrackerCZ.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerCZ.Services
{
    public class TaxCalculationService
    {
        private readonly AppDbContext _context;

        public TaxCalculationService(AppDbContext context)
        {
            _context = context;
        }

        public static DateTime GetTaxYearStart(int taxYear)
        {
            return new DateTime(taxYear, 4, 6);
        }

        public static DateTime GetTaxYearEnd(int taxYear)
        {
            return new DateTime(taxYear + 1, 4, 5);
        }

        public static int GetCurrentTaxYear()
        {
            var now = DateTime.Now;
            return now.Month >= 4 && now.Day >= 6 ? now.Year : now.Year - 1;
        }

        public static int GetTaxYearForDate(DateTime date)
        {
            return date.Month >= 4 && date.Day >= 6 ? date.Year : date.Year - 1;
        }

        public async Task<List<TaxWarning>> GetActiveTaxWarningsAsync()
        {
            var currentTaxYear = GetCurrentTaxYear();
            
            var warnings = new List<TaxWarning>();
            
            // Check interest income warning
            var interestWarning = await CheckInterestIncomeWarningAsync(currentTaxYear);
            if (interestWarning != null)
            {
                warnings.Add(interestWarning);
            }
            
            // Check capital gains warning (if applicable)
            var capitalGainsWarning = await CheckCapitalGainsWarningAsync(currentTaxYear);
            if (capitalGainsWarning != null)
            {
                warnings.Add(capitalGainsWarning);
            }

            return warnings;
        }

        private async Task<TaxWarning?> CheckInterestIncomeWarningAsync(int taxYear)
        {
            var taxYearStart = GetTaxYearStart(taxYear);
            var taxYearEnd = GetTaxYearEnd(taxYear);

            // Get all interest income transactions in the current tax year
            var interestTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Category != null && 
                           t.Category.Type == "Income" && 
                           (t.Category.Title.ToLower().Contains("interest") || 
                            t.Category.Title.ToLower().Contains("savings")) &&
                           t.Date >= taxYearStart && t.Date <= taxYearEnd)
                .ToListAsync();

            var totalInterest = interestTransactions.Sum(t => t.Amount);
            var threshold = 1000m; // £1000 tax-free allowance

            if (totalInterest > 0)
            {
                var severity = totalInterest >= threshold ? "Critical" :
                              totalInterest >= threshold * 0.75m ? "High" :
                              totalInterest >= threshold * 0.5m ? "Medium" : "Low";

                var message = totalInterest >= threshold 
                    ? $"You have exceeded the £1,000 tax-free savings allowance by £{totalInterest - threshold:F2}. You may need to pay tax on interest above this threshold."
                    : $"You have earned £{totalInterest:F2} in interest income. £{threshold - totalInterest:F2} remaining in your tax-free allowance.";

                return new TaxWarning
                {
                    Type = "Interest",
                    Message = message,
                    Threshold = threshold,
                    CurrentAmount = totalInterest,
                    TaxYear = taxYear,
                    Severity = severity,
                    IsActive = true
                };
            }

            return null;
        }

        private async Task<TaxWarning?> CheckCapitalGainsWarningAsync(int taxYear)
        {
            var taxYearStart = GetTaxYearStart(taxYear);
            var taxYearEnd = GetTaxYearEnd(taxYear);

            // Get all capital gains transactions in the current tax year
            var capitalGainsTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Category != null && 
                           t.Category.Type == "Income" && 
                           (t.Category.Title.ToLower().Contains("capital gains") || 
                            t.Category.Title.ToLower().Contains("investment gains") ||
                            t.Category.Title.ToLower().Contains("profit")) &&
                           t.Date >= taxYearStart && t.Date <= taxYearEnd)
                .ToListAsync();

            var totalCapitalGains = capitalGainsTransactions.Sum(t => t.Amount);
            var threshold = 3000m; // £3000 capital gains allowance for 2025-26

            if (totalCapitalGains > 0)
            {
                var severity = totalCapitalGains >= threshold ? "Critical" :
                              totalCapitalGains >= threshold * 0.8m ? "High" :
                              totalCapitalGains >= threshold * 0.6m ? "Medium" : "Low";

                var message = totalCapitalGains >= threshold 
                    ? $"You have exceeded the £3000 capital gains allowance by £{totalCapitalGains - threshold:F2}. You may need to pay capital gains tax."
                    : $"You have realized £{totalCapitalGains:F2} in capital gains. £{threshold - totalCapitalGains:F2} remaining in your annual allowance.";

                return new TaxWarning
                {
                    Type = "Capital Gains",
                    Message = message,
                    Threshold = threshold,
                    CurrentAmount = totalCapitalGains,
                    TaxYear = taxYear,
                    Severity = severity,
                    IsActive = true
                };
            }

            return null;
        }

        public async Task<Dictionary<string, decimal>> GetTaxYearSummaryAsync(int taxYear)
        {
            var taxYearStart = GetTaxYearStart(taxYear);
            var taxYearEnd = GetTaxYearEnd(taxYear);

            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= taxYearStart && t.Date <= taxYearEnd)
                .ToListAsync();

            var summary = new Dictionary<string, decimal>
            {
                ["TotalIncome"] = transactions
                    .Where(t => t.Category?.Type == "Income")
                    .Sum(t => t.Amount),
                ["TotalExpenses"] = transactions
                    .Where(t => t.Category?.Type == "Expense")
                    .Sum(t => t.Amount),
                ["InterestIncome"] = transactions
                    .Where(t => t.Category?.Type == "Income" && 
                               (t.Category.Title.ToLower().Contains("interest") || 
                                t.Category.Title.ToLower().Contains("savings")))
                    .Sum(t => t.Amount),
                ["CapitalGains"] = transactions
                    .Where(t => t.Category?.Type == "Income" && 
                               (t.Category.Title.ToLower().Contains("capital gains") || 
                                t.Category.Title.ToLower().Contains("investment gains") ||
                                t.Category.Title.ToLower().Contains("profit")))
                    .Sum(t => t.Amount)
            };

            return summary;
        }
    }
}