namespace PayrollManagementSystemMVC.Models
{
    public class Payroll
    {
        public int PayrollId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public decimal GrossPay { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }
        public string Status { get; set; } = "Pending"; // Pending / Approved / Rejected
        public DateTime CreatedAt { get; set; }

        // Display-only, not stored directly
        public string? EmployeeName { get; set; }
        public int DaysWorked { get; set; }
        public int Absences { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal LateUndertimeHours { get; set; }
    }
}