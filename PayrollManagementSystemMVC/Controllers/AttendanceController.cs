using Microsoft.AspNetCore.Mvc;
using PayrollManagementSystemMVC.Models;
using PayrollManagementSystemMVC.Services;

namespace PayrollManagementSystemMVC.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly AttendanceService _attendanceService = new AttendanceService();
        private readonly EmployeeService _employeeService = new EmployeeService();

        public IActionResult Index()
        {
            return View(_attendanceService.GetAllAttendance());
        }

        public IActionResult Create()
        {
            ViewBag.Employees = _employeeService.GetAllEmployees();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Attendance attendance)
        {
            _attendanceService.RecordAttendance(attendance);
            return RedirectToAction("Index");
        }

        public IActionResult Summary(int employeeId)
        {
            var records = _attendanceService.GetAttendanceByEmployee(employeeId);
            var employee = _employeeService.GetEmployeeById(employeeId);

            ViewBag.EmployeeId = employeeId;
            ViewBag.EmployeeName = employee != null ? $"{employee.FirstName} {employee.LastName}" : "";
            ViewBag.TotalDaysWorked = records.Count(r => r.Status != "Absent");
            ViewBag.TotalAbsences = records.Count(r => r.Status == "Absent");
            ViewBag.TotalLateHours = records.Sum(r => r.LateHours);
            ViewBag.TotalUndertimeHours = records.Sum(r => r.UndertimeHours);
            ViewBag.TotalOvertimeHours = records.Sum(r => r.OvertimeHours);

            return View(records);
        }
    }
}
