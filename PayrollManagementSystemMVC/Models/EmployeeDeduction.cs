namespace PayrollManagementSystemMVC.Models
{
    public class EmployeeDeduction
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int DeductionId { get; set; }
        public decimal? CustomAmount { get; set; }
        public bool IsActive { get; set; } = true;
        public string? EmployeeName { get; set; }
        public string? DeductionName { get; set; }
        public string? DeductionType { get; set; }
        public decimal Amount { get; set; }
        public decimal ComputedAmount { get; set; }
    }
}
