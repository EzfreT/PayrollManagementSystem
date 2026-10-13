using MySqlConnector;
using PayrollManagementSystemMVC.Models;
using PayrollManagementSystemMVC.Models.Database;

namespace PayrollManagementSystemMVC.Services
{
    public class PayrollService
    {
        private const int StandardWorkingDaysPerMonth = 22;
        private const decimal OvertimeMultiplier = 1.25m;

        public int GetPayrollCountForEmployee(int employeeId)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            string query = "SELECT COUNT(*) FROM payroll WHERE employee_id = @empId";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@empId", employeeId);
            long count = (long)cmd.ExecuteScalar()!;
            return (int)count;
        }

        public string? ValidatePayrollRun(int employeeId, DateTime periodStart, DateTime periodEnd)
        {
            if (periodStart == default || periodEnd == default)
                return "Both pay period dates are required.";

            if (periodEnd < periodStart)
                return "Pay period end date must be after the start date.";

            using var conn = DbConnection.GetConnection();
            conn.Open();

            string empCheck = "SELECT COUNT(*) FROM employees WHERE employee_id = @empId";
            using var empCmd = new MySqlCommand(empCheck, conn);
            empCmd.Parameters.AddWithValue("@empId", employeeId);
            long empCount = (long)empCmd.ExecuteScalar()!;
            if (empCount == 0) return "Selected employee does not exist.";

            string salaryCheck = "SELECT COUNT(*) FROM salary WHERE employee_id = @empId AND is_deleted = 0";
            using var salCmd = new MySqlCommand(salaryCheck, conn);
            salCmd.Parameters.AddWithValue("@empId", employeeId);
            long salCount = (long)salCmd.ExecuteScalar()!;
            if (salCount == 0) return "This employee has no salary record assigned yet.";

            string overlapCheck = @"SELECT COUNT(*) FROM payroll
                WHERE employee_id = @empId
                  AND pay_period_start = @start AND pay_period_end = @end";
            using var overlapCmd = new MySqlCommand(overlapCheck, conn);
            overlapCmd.Parameters.AddWithValue("@empId", employeeId);
            overlapCmd.Parameters.AddWithValue("@start", periodStart);
            overlapCmd.Parameters.AddWithValue("@end", periodEnd);
            long overlapCount = (long)overlapCmd.ExecuteScalar()!;
            if (overlapCount > 0) return "A payroll record already exists for this employee and pay period.";

            return null;
        }

        public Payroll ProcessPayroll(int employeeId, DateTime periodStart, DateTime periodEnd)
        {
            var salaryService = new SalaryService();
            var attendanceService = new AttendanceService();
            var deductionService = new DeductionService();

            var currentSalary = salaryService.GetCurrentSalaryForEmployee(employeeId);
            decimal basicSalary = currentSalary?.BasicSalary ?? 0;
            decimal dailyRate = basicSalary / StandardWorkingDaysPerMonth;
            decimal hourlyRate = dailyRate / 8m;

            var allAttendance = attendanceService.GetAttendanceByEmployee(employeeId);
            var periodAttendance = allAttendance
                .Where(a => a.AttendanceDate.Date >= periodStart.Date && a.AttendanceDate.Date <= periodEnd.Date)
                .ToList();

            int daysWorked = periodAttendance.Count(a => a.Status != "Absent");
            int absences = periodAttendance.Count(a => a.Status == "Absent");
            decimal totalOvertimeHours = periodAttendance.Sum(a => a.OvertimeHours);
            decimal totalLateUndertimeHours = periodAttendance.Sum(a => a.LateHours + a.UndertimeHours);

            decimal basePay = dailyRate * daysWorked;
            decimal overtimePay = totalOvertimeHours * hourlyRate * OvertimeMultiplier;
            decimal lateUndertimeDeduction = totalLateUndertimeHours * hourlyRate;

            decimal grossPay = basePay + overtimePay - lateUndertimeDeduction;
            if (grossPay < 0) grossPay = 0;

            decimal totalDeductions = deductionService.GetTotalDeductions(employeeId, basicSalary);

            decimal netPay = grossPay - totalDeductions;
            if (netPay < 0) netPay = 0;

            var payroll = new Payroll
            {
                EmployeeId = employeeId,
                PayPeriodStart = periodStart,
                PayPeriodEnd = periodEnd,
                GrossPay = Math.Round(grossPay, 2),
                TotalDeductions = Math.Round(totalDeductions, 2),
                NetPay = Math.Round(netPay, 2),
                Status = "Pending",
                DaysWorked = daysWorked,
                Absences = absences,
                OvertimeHours = totalOvertimeHours,
                LateUndertimeHours = totalLateUndertimeHours
            };

            SavePayroll(payroll);
            return payroll;
        }

        private void SavePayroll(Payroll p)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"INSERT INTO payroll
                (employee_id, pay_period_start, pay_period_end, gross_pay, total_deductions, net_pay, status, created_at)
                VALUES (@empId, @start, @end, @gross, @deductions, @net, @status, NOW())";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@empId", p.EmployeeId);
            cmd.Parameters.AddWithValue("@start", p.PayPeriodStart);
            cmd.Parameters.AddWithValue("@end", p.PayPeriodEnd);
            cmd.Parameters.AddWithValue("@gross", p.GrossPay);
            cmd.Parameters.AddWithValue("@deductions", p.TotalDeductions);
            cmd.Parameters.AddWithValue("@net", p.NetPay);
            cmd.Parameters.AddWithValue("@status", p.Status);
            cmd.ExecuteNonQuery();

            p.PayrollId = (int)cmd.LastInsertedId;
        }

        public List<Payroll> GetAllPayrolls()
        {
            var list = new List<Payroll>();
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"
                SELECT p.payroll_id, p.employee_id, p.pay_period_start, p.pay_period_end,
                       p.gross_pay, p.total_deductions, p.net_pay, p.status, p.created_at,
                       e.first_name, e.last_name
                FROM payroll p
                INNER JOIN employees e ON p.employee_id = e.employee_id
                ORDER BY p.created_at DESC";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToPayroll(reader));
            }
            return list;
        }

        public Payroll? GetPayrollById(int payrollId)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"
                SELECT p.payroll_id, p.employee_id, p.pay_period_start, p.pay_period_end,
                       p.gross_pay, p.total_deductions, p.net_pay, p.status, p.created_at,
                       e.first_name, e.last_name
                FROM payroll p
                INNER JOIN employees e ON p.employee_id = e.employee_id
                WHERE p.payroll_id = @id";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", payrollId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var payroll = MapReaderToPayroll(reader);
                reader.Close();
                FillAttendanceBreakdown(payroll);
                return payroll;
            }
            return null;
        }

        // Days Worked / Absences / Overtime / Late+Undertime aren't stored columns on
        // payroll (only the money fields are). Recompute them from Attendance using the
        // saved pay period, so the Details page is correct even after a reload (e.g. Approve).
        private void FillAttendanceBreakdown(Payroll payroll)
        {
            var attendanceService = new AttendanceService();
            var periodAttendance = attendanceService.GetAttendanceByEmployee(payroll.EmployeeId)
                .Where(a => a.AttendanceDate.Date >= payroll.PayPeriodStart.Date
                         && a.AttendanceDate.Date <= payroll.PayPeriodEnd.Date)
                .ToList();

            payroll.DaysWorked = periodAttendance.Count(a => a.Status != "Absent");
            payroll.Absences = periodAttendance.Count(a => a.Status == "Absent");
            payroll.OvertimeHours = periodAttendance.Sum(a => a.OvertimeHours);
            payroll.LateUndertimeHours = periodAttendance.Sum(a => a.LateHours + a.UndertimeHours);
        }

        public void UpdateStatus(int payrollId, string status)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            string query = "UPDATE payroll SET status = @status WHERE payroll_id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", payrollId);
            cmd.ExecuteNonQuery();
        }

        private Payroll MapReaderToPayroll(MySqlDataReader reader)
        {
            return new Payroll
            {
                PayrollId = reader.GetInt32(reader.GetOrdinal("payroll_id")),
                EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
                PayPeriodStart = reader.GetDateTime(reader.GetOrdinal("pay_period_start")),
                PayPeriodEnd = reader.GetDateTime(reader.GetOrdinal("pay_period_end")),
                GrossPay = reader.GetDecimal(reader.GetOrdinal("gross_pay")),
                TotalDeductions = reader.GetDecimal(reader.GetOrdinal("total_deductions")),
                NetPay = reader.GetDecimal(reader.GetOrdinal("net_pay")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                EmployeeName = reader.GetString(reader.GetOrdinal("first_name")) + " " +
                               reader.GetString(reader.GetOrdinal("last_name"))
            };
        }
    }
}