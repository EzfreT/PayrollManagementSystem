namespace PayrollManagementSystemMVC.Models
{
    public class Salary
    {
        public int SalaryId { get; set; }
        public int EmployeeId { get; set; }
        public decimal BasicSalary { get; set; }
        public string PayFrequency { get; set; } = "Monthly";
        public DateTime EffectiveDate { get; set; }
        public string? EmployeeName { get; set; }
        public decimal? PreviousSalary { get; set; }
        public bool IsDeleted { get; set; }
    }
}
