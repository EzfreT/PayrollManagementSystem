using Microsoft.AspNetCore.Mvc;
using PayrollManagementSystemMVC.Models;
using PayrollManagementSystemMVC.Services;

namespace PayrollManagementSystemMVC.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly EmployeeService _employeeService = new EmployeeService();

        public IActionResult Index(string? search)
        {
            var employees = string.IsNullOrWhiteSpace(search)
                ? _employeeService.GetAllEmployees()
                : _employeeService.SearchEmployees(search);

            ViewBag.Search = search;
            return View(employees);
        }

        public IActionResult Details(int id)
        {
            var employee = _employeeService.GetEmployeeById(id);
            if (employee == null) return NotFound();

            var salaryService = new SalaryService();
            var payrollService = new PayrollService();

            var currentSalary = salaryService.GetCurrentSalaryForEmployee(id);
            ViewBag.BasicSalary = currentSalary?.BasicSalary ?? 0;
            ViewBag.TotalPayrolls = payrollService.GetPayrollCountForEmployee(id);

            return View(employee);
        }

        public IActionResult Create()
        {
            var employee = new Employee
            {
                EmployeeNumber = _employeeService.GetNextEmployeeNumber()
            };
            return View(employee);
        }

        [HttpPost]
        public IActionResult Create(Employee employee)
        {
            var error = _employeeService.ValidateEmployee(employee);
            if (error != null)
            {
                ViewBag.Error = error;
                return View(employee);
            }

            _employeeService.AddEmployee(employee);
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var employee = _employeeService.GetEmployeeById(id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        [HttpPost]
        public IActionResult Edit(Employee employee)
        {
            var error = _employeeService.ValidateEmployee(employee);
            if (error != null)
            {
                ViewBag.Error = error;
                return View(employee);
            }

            _employeeService.UpdateEmployee(employee);
            return RedirectToAction("Index");
        }

        public IActionResult Deactivate(int id)
        {
            var employee = _employeeService.GetEmployeeById(id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        [HttpPost, ActionName("Deactivate")]
        public IActionResult DeactivateConfirmed(int id)
        {
            _employeeService.DeactivateEmployee(id);
            return RedirectToAction("Index");
        }

        public IActionResult Reactivate(int id)
        {
            _employeeService.ReactivateEmployee(id);
            return RedirectToAction("Index");
        }
    }
}
