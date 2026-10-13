using Microsoft.AspNetCore.Mvc;
using PayrollManagementSystemMVC.Models;
using PayrollManagementSystemMVC.Services;

namespace PayrollManagementSystemMVC.Controllers
{
    public class DeductionController : Controller
    {
        private readonly DeductionService _deductionService = new DeductionService();
        private readonly EmployeeService _employeeService = new EmployeeService();
        private readonly SalaryService _salaryService = new SalaryService();

        public IActionResult Index()
        {
            return View(_deductionService.GetAllDeductions());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Deduction deduction)
        {
            _deductionService.AddDeduction(deduction);
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            _deductionService.DeleteDeduction(id);
            return RedirectToAction("Index");
        }

        public IActionResult Assignments()
        {
            return View(_deductionService.GetAllAssignments());
        }

        public IActionResult Assign()
        {
            ViewBag.Employees = _employeeService.GetAllEmployees();
            ViewBag.Deductions = _deductionService.GetAllDeductions();
            return View();
        }

        [HttpPost]
        public IActionResult Assign(int employeeId, int deductionId, decimal? customAmount)
        {
            _deductionService.AssignDeduction(employeeId, deductionId, customAmount);
            return RedirectToAction("Assignments");
        }

        public IActionResult Edit(int id)
        {
            var assignment = _deductionService.GetAssignmentById(id);
            if (assignment == null) return NotFound();
            return View(assignment);
        }

        [HttpPost]
        public IActionResult Edit(int id, decimal? customAmount)
        {
            _deductionService.EditAssignment(id, customAmount);
            return RedirectToAction("Assignments");
        }

        public IActionResult Deactivate(int id)
        {
            _deductionService.DeactivateAssignment(id);
            return RedirectToAction("Assignments");
        }

        public IActionResult Reactivate(int id)
        {
            _deductionService.ReactivateAssignment(id);
            return RedirectToAction("Assignments");
        }

        public IActionResult History(int id)
        {
            ViewBag.Assignment = _deductionService.GetAssignmentById(id);
            return View(_deductionService.GetAssignmentHistory(id));
        }

        public IActionResult Computed(int employeeId)
        {
            var employee = _employeeService.GetEmployeeById(employeeId);
            if (employee == null) return NotFound();

            var currentSalary = _salaryService.GetCurrentSalaryForEmployee(employeeId);
            decimal basicSalary = currentSalary?.BasicSalary ?? 0;

            ViewBag.EmployeeName = $"{employee.FirstName} {employee.LastName}";
            ViewBag.BasicSalary = basicSalary;
            ViewBag.Total = _deductionService.GetTotalDeductions(employeeId, basicSalary);

            return View(_deductionService.ComputeDeductionsForEmployee(employeeId, basicSalary));
        }
    }
}
