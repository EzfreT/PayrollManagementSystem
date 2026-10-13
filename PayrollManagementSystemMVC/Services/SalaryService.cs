using MySqlConnector;
using PayrollManagementSystemMVC.Models;
using PayrollManagementSystemMVC.Models.Database;

namespace PayrollManagementSystemMVC.Services
{
    public class SalaryService
    {
        public List<Salary> GetCurrentSalaries()
        {
            var list = new List<Salary>();
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"
                SELECT s.salary_id, s.employee_id, s.basic_salary, s.pay_frequency, s.effective_date, s.is_deleted,
                       e.first_name, e.last_name
                FROM salary s
                INNER JOIN employees e ON s.employee_id = e.employee_id
                WHERE s.is_deleted = 0
                  AND s.salary_id = (
                        SELECT MAX(salary_id) FROM salary
                        WHERE employee_id = s.employee_id AND is_deleted = 0
                  )
                ORDER BY e.last_name";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToSalary(reader));
            }
            return list;
        }

        public List<Salary> GetSalaryHistory(int employeeId, bool includeDeleted = false)
        {
            var list = new List<Salary>();
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"
                SELECT s.salary_id, s.employee_id, s.basic_salary, s.pay_frequency, s.effective_date, s.is_deleted,
                       e.first_name, e.last_name
                FROM salary s
                INNER JOIN employees e ON s.employee_id = e.employee_id
                WHERE s.employee_id = @empId" + (includeDeleted ? "" : " AND s.is_deleted = 0") + @"
                ORDER BY s.effective_date DESC";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@empId", employeeId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToSalary(reader));
            }

            // Fill in PreviousSalary by looking at the next-older record in the (desc-ordered) list
            for (int i = 0; i < list.Count - 1; i++)
            {
                list[i].PreviousSalary = list[i + 1].BasicSalary;
            }

            return list;
        }

        public Salary? GetSalaryById(int salaryId)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"
                SELECT s.salary_id, s.employee_id, s.basic_salary, s.pay_frequency, s.effective_date, s.is_deleted,
                       e.first_name, e.last_name
                FROM salary s
                INNER JOIN employees e ON s.employee_id = e.employee_id
                WHERE s.salary_id = @id";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", salaryId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapReaderToSalary(reader);
            }
            return null;
        }

        public Salary? GetCurrentSalaryForEmployee(int employeeId)
        {
            return GetSalaryHistory(employeeId).FirstOrDefault(); // already ordered by effective_date DESC
        }

        public string? ValidateSalary(Salary salary, bool isNewRecord)
        {
            using (var conn = DbConnection.GetConnection())
            {
                conn.Open();
                string checkQuery = "SELECT COUNT(*) FROM employees WHERE employee_id = @empId";
                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@empId", salary.EmployeeId);
                long empCount = (long)checkCmd.ExecuteScalar()!;
                if (empCount == 0) return "Selected employee does not exist.";
            }

            if (salary.BasicSalary <= 0)
                return "Basic salary must be greater than zero.";

            if (salary.EffectiveDate == default)
                return "Effective date is required.";

            if (isNewRecord)
            {
                var history = GetSalaryHistory(salary.EmployeeId);
                if (history.Any(h => h.EffectiveDate >= salary.EffectiveDate))
                    return "Effective date must be later than the employee's most recent salary record.";
            }

            return null;
        }

        public void AssignSalary(Salary salary)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"INSERT INTO salary (employee_id, basic_salary, pay_frequency, effective_date, is_deleted)
                              VALUES (@empId, @amount, @freq, @date, 0)";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@empId", salary.EmployeeId);
            cmd.Parameters.AddWithValue("@amount", salary.BasicSalary);
            cmd.Parameters.AddWithValue("@freq", salary.PayFrequency);
            cmd.Parameters.AddWithValue("@date", salary.EffectiveDate);
            cmd.ExecuteNonQuery();
        }

        public void UpdateSalary(Salary salary)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"UPDATE salary SET
                basic_salary = @amount, pay_frequency = @freq, effective_date = @date
                WHERE salary_id = @id";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@amount", salary.BasicSalary);
            cmd.Parameters.AddWithValue("@freq", salary.PayFrequency);
            cmd.Parameters.AddWithValue("@date", salary.EffectiveDate);
            cmd.Parameters.AddWithValue("@id", salary.SalaryId);
            cmd.ExecuteNonQuery();
        }

        public void DeleteSalary(int salaryId)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            string query = "UPDATE salary SET is_deleted = 1 WHERE salary_id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", salaryId);
            cmd.ExecuteNonQuery();
        }

        public void RestoreSalary(int salaryId)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            string query = "UPDATE salary SET is_deleted = 0 WHERE salary_id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", salaryId);
            cmd.ExecuteNonQuery();
        }

        private Salary MapReaderToSalary(MySqlDataReader reader)
        {
            return new Salary
            {
                SalaryId = reader.GetInt32(reader.GetOrdinal("salary_id")),
                EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
                BasicSalary = reader.GetDecimal(reader.GetOrdinal("basic_salary")),
                PayFrequency = reader.GetString(reader.GetOrdinal("pay_frequency")),
                EffectiveDate = reader.GetDateTime(reader.GetOrdinal("effective_date")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("is_deleted")),
                EmployeeName = reader.GetString(reader.GetOrdinal("first_name")) + " " +
                               reader.GetString(reader.GetOrdinal("last_name"))
            };
        }
    }
}
