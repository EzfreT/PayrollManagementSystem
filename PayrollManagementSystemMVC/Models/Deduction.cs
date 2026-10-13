namespace PayrollManagementSystemMVC.Models
{
    public class Deduction
    {
        public int DeductionId { get; set; }
        public string DeductionName { get; set; } = string.Empty;
        public string DeductionType { get; set; } = "Fixed"; // "Fixed" or "Percentage"
        public decimal Amount { get; set; }
    }
}
