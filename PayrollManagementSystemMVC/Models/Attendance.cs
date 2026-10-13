namespace PayrollManagementSystemMVC.Models
{
    public class Attendance
    {
        public int AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public decimal? HoursWorked { get; set; }
        public string Status { get; set; } = "Present";
        public bool IsAbsent { get; set; }
        public decimal LateHours { get; set; }
        public decimal UndertimeHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public string? EmployeeName { get; set; }
    }
}
