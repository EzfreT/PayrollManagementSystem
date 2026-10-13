using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using PayrollManagementSystemMVC.Models;
using PayrollManagementSystemMVC.Models.Database;
using PayrollManagementSystemMVC.Services;

namespace PayrollManagementSystemMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly EmployeeService _employeeService = new EmployeeService();
        private readonly SalaryService _salaryService = new SalaryService();
        private readonly AttendanceService _attendanceService = new AttendanceService();
        private readonly PayrollService _payrollService = new PayrollService();

        public IActionResult Index()
        {
            try
            {
                using var conn = DbConnection.GetConnection();
                conn.Open();
                ViewBag.DbStatus = "Connected to MariaDB successfully!";
            }
            catch (Exception ex)
            {
                ViewBag.DbStatus = "Database connection failed: " + ex.Message;
                ViewBag.DbError = true;
                return View();
            }

            var allEmployees = _employeeService.GetAllEmployees();
            ViewBag.TotalEmployees = allEmployees.Count;
            ViewBag.ActiveEmployees = allEmployees.Count(e => e.Status == "Active");
            ViewBag.InactiveEmployees = allEmployees.Count(e => e.Status != "Active");

            var allPayrolls = _payrollService.GetAllPayrolls();
            ViewBag.TotalPayrolls = allPayrolls.Count;
            ViewBag.PendingPayrolls = allPayrolls.Count(p => p.Status == "Pending");
            ViewBag.NetPayDisbursed = allPayrolls.Where(p => p.Status == "Approved").Sum(p => p.NetPay);

            var allAttendance = _attendanceService.GetAllAttendance();
            var today = DateTime.Today;
            var todayAttendance = allAttendance.Where(a => a.AttendanceDate.Date == today).ToList();
            ViewBag.TodayPresent = todayAttendance.Count(a => a.Status != "Absent");
            ViewBag.TodayAbsent = todayAttendance.Count(a => a.Status == "Absent");

            ViewBag.RecentPayrolls = allPayrolls.Take(5).ToList();

            var employeesMissingSalary = allEmployees
                .Where(e => e.Status == "Active" && _salaryService.GetCurrentSalaryForEmployee(e.EmployeeId) == null)
                .ToList();
            ViewBag.EmployeesMissingSalary = employeesMissingSalary;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}