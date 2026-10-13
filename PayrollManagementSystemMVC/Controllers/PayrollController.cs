using Microsoft.AspNetCore.Mvc;
using PayrollManagementSystemMVC.Services;

namespace PayrollManagementSystemMVC.Controllers
{
    public class PayrollController : Controller
    {
        private readonly PayrollService _payrollService = new PayrollService();
        private readonly EmployeeService _employeeService = new EmployeeService();

        public IActionResult Index()
        {
            return View(_payrollService.GetAllPayrolls());
        }

        public IActionResult Create()
        {
            ViewBag.Employees = _employeeService.GetAllEmployees();
            return View();
        }

        [HttpPost]
        public IActionResult Create(int employeeId, DateTime payPeriodStart, DateTime payPeriodEnd)
        {
            var error = _payrollService.ValidatePayrollRun(employeeId, payPeriodStart, payPeriodEnd);
            if (error != null)
            {
                ViewBag.Error = error;
                ViewBag.Employees = _employeeService.GetAllEmployees();
                return View();
            }

            var payroll = _payrollService.ProcessPayroll(employeeId, payPeriodStart, payPeriodEnd);
            return RedirectToAction("Details", new { id = payroll.PayrollId });
        }

        public IActionResult Details(int id)
        {
            var payroll = _payrollService.GetPayrollById(id);
            if (payroll == null) return NotFound();
            return View(payroll);
        }

        public IActionResult Approve(int id)
        {
            _payrollService.UpdateStatus(id, "Approved");
            return RedirectToAction("Details", new { id });
        }

        public IActionResult Reject(int id)
        {
            _payrollService.UpdateStatus(id, "Rejected");
            return RedirectToAction("Details", new { id });
        }
    }
}