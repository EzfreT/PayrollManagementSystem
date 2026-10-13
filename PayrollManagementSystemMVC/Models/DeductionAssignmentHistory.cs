namespace PayrollManagementSystemMVC.Models
{
    public class DeductionAssignmentHistory
    {
        public int HistoryId { get; set; }
        public int EmployeeDeductionId { get; set; }
        public string Action { get; set; } = string.Empty;
        public decimal? CustomAmount { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
