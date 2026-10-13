using Microsoft.AspNetCore.Mvc;
using PayrollManagementSystemMVC.Models;
using PayrollManagementSystemMVC.Services;

namespace PayrollManagementSystemMVC.Controllers
{
    public class SalaryController : Controller
    {
        private readonly SalaryService _salaryService = new SalaryService();
        private readonly EmployeeService _employeeService = new EmployeeService();

        public IActionResult Index()
        {
            return View(_salaryService.GetCurrentSalaries());
        }

        public IActionResult History(int employeeId)
        {
            ViewBag.EmployeeId = employeeId;
            var employee = _employeeService.GetEmployeeById(employeeId);
            ViewBag.EmployeeName = employee != null ? $"{employee.FirstName} {employee.LastName}" : "";
            return View(_salaryService.GetSalaryHistory(employeeId));
        }

        public IActionResult Create()
        {
            ViewBag.Employees = _employeeService.GetAllEmployees();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Salary salary)
        {
            var error = _salaryService.ValidateSalary(salary, isNewRecord: true);
            if (error != null)
            {
                ViewBag.Error = error;
                ViewBag.Employees = _employeeService.GetAllEmployees();
                return View(salary);
            }

            _salaryService.AssignSalary(salary);
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var salary = _salaryService.GetSalaryById(id);
            if (salary == null) return NotFound();
            return View(salary);
        }

        [HttpPost]
        public IActionResult Edit(Salary salary)
        {
            var error = _salaryService.ValidateSalary(salary, isNewRecord: false);
            if (error != null)
            {
                ViewBag.Error = error;
                return View(salary);
            }

            _salaryService.UpdateSalary(salary);
            return RedirectToAction("History", new { employeeId = salary.EmployeeId });
        }

        public IActionResult Delete(int id, int employeeId)
        {
            _salaryService.DeleteSalary(id);
            return RedirectToAction("History", new { employeeId });
        }

        public IActionResult Deleted(int employeeId)
        {
            ViewBag.EmployeeId = employeeId;
            var all = _salaryService.GetSalaryHistory(employeeId, includeDeleted: true);
            return View(all.Where(s => s.IsDeleted).ToList());
        }

        public IActionResult Restore(int id, int employeeId)
        {
            _salaryService.RestoreSalary(id);
            return RedirectToAction("History", new { employeeId });
        }
    }
}
