Name: BudgetTrackerCZ (Personal finance management system built in ASP.NET Core)
built with ASP.NET Core MVC and Entity Framework Core. Track your income, expenses, set financial targets, and monitor tax obligations with an intuitive dashboard and automated notifications.

Features

Core Functionality
Transaction Management: Record income and expense transactions with categories and notes
Category Management: Organize transactions with customizable categories and icons
Dashboard Analytics: Visual charts showing spending patterns, income vs expenses, and recent activity
Financial Targets: Set monthly budget goals for income/expense categories with progress tracking

Advanced Features
Tax Calculations: UK tax year-based calculations with automated warnings for:
  - Interest income thresholds (£1,000 allowance)
  - Capital gains thresholds (£3,000 allowance)
Reminders System: Set and track financial reminders with due dates
Smart Notifications: Automated system notifications for budget alerts and targets
Data Visualization: Interactive charts using Syncfusion components for expense breakdowns and trends

Technology Stack

Backend: ASP.NET Core 8.0 MVC
Database: SQLite with Entity Framework Core 8.0
Frontend: HTML, CSS, JavaScript with Bootstrap
Charts: Syncfusion EJ2 components
Database Migrations: EF Core migrations for schema management

Project Structure


BudgetTrackerCZ/
├── Controllers/           # MVC Controllers
│   ├── DashboardController.cs
│   ├── TransactionController.cs
│   ├── CategoryController.cs
│   ├── FinancialTargetController.cs
│   ├── TaxController.cs
│   ├── ReminderController.cs
│   └── NotificationController.cs
├── Models/               # Data models
│   ├── Transaction.cs
│   ├── Category.cs
│   ├── FinancialTarget.cs
│   ├── TaxWarning.cs
│   ├── Reminder.cs
│   └── Notification.cs
├── Services/             # Business logic services
│   ├── TaxCalculationService.cs
│   ├── NotificationService.cs
│   └── FinancialTargetService.cs
├── Views/                # MVC Views
│   ├── Dashboard/
│   ├── Transaction/
│   ├── Category/
│   ├── FinancialTarget/
│   ├── Tax/
│   ├── Reminder/
│   └── Shared/
├── wwwroot/              # Static files (CSS, JS, images)
└── Migrations/           # EF Core database migrations
Getting Started

Prerequisites
.NET 8.0 SDK
Visual Studio 2022

Installation

1. Clone the repository
   git clone https://github.com/ClaireZu/BudgetTrackerCZ.git
   cd BudgetTrackerCZ
   

2. Restore NuGet packages
   
   dotnet restore
   

3. Update database
   
   dotnet ef database update
   

4. Run the application
   bash
   dotnet run
   

5. Access the application
   - Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

Usage

Dashboard
The main dashboard provides an overview of your financial status including:
- Last 7 days income, expenses, and balance
- Expense breakdown by category (doughnut chart)
- Income vs expense trends (spline chart)
- Recent transactions
- Active reminders and tax warnings

Transaction Management
- Add income and expense transactions
- Categorize transactions with custom categories and icons
- View transaction history with filtering options
- Edit or delete existing transactions

Financial Targets
- Set monthly budget targets for specific categories
- Track progress with visual indicators
- Monitor variance from targets
- Automatic status updates (On Track, Behind Target, Over Budget, Target Achieved)

Tax Features
- Automatic calculation of UK tax year periods (April 6 - April 5)
- Interest income tracking against £1,000 tax-free allowance
- Capital gains monitoring against £3,000 annual allowance
- Tax warning notifications with severity levels
- Historical tax year summaries

Reminders & Notifications
- Create financial reminders with due dates
- Automatic system notifications for important events
- Mark reminders as completed
- Dashboard integration for upcoming reminders

Database Schema

The application uses SQLite with the following main entities:
Transactions: Core financial records with amount, date, category, and notes
Categories: Transaction categorization with type (Income/Expense) and icons
FinancialTargets: Monthly budget goals with progress tracking
TaxWarnings: Automated tax threshold notifications
Reminders: User-defined financial reminders
Notifications: System-generated alerts and messages

Configuration

Database Connection
The application uses SQLite by default with the connection string configured in `Program.cs`:

options.UseSqlite("Data Source=budget.db")


Syncfusion License
The application requires a Syncfusion license for chart components. Update the license key in `Program.cs`:

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("YOUR_LICENSE_KEY");


Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/new-feature`)
3. Commit your changes (`git commit -am 'Add new feature'`)
4. Push to the branch (`git push origin feature/new-feature`)
5. Create a Pull Request

License

This project is licensed under the MIT License - see the LICENSE file for details.

Support

For support, please create an issue on the GitHub repository or contact the development team.

Acknowledgments

- Built with ASP.NET Core and Entity Framework Core
- UI components by Syncfusion
- Icons and styling with Bootstrap
- UK tax calculations based on HMRC guidelines
