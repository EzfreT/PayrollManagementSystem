using MySqlConnector;
using PayrollManagementSystemMVC.Models;
using PayrollManagementSystemMVC.Models.Database;

namespace PayrollManagementSystemMVC.Services
{
    public class DeductionService
    {
        public List<Deduction> GetAllDeductions()
        {
            var list = new List<Deduction>();
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = "SELECT * FROM deductions ORDER BY deduction_name";
            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Deduction
                {
                    DeductionId = reader.GetInt32(reader.GetOrdinal("deduction_id")),
                    DeductionName = reader.GetString(reader.GetOrdinal("deduction_name")),
                    DeductionType = reader.GetString(reader.GetOrdinal("deduction_type")),
                    Amount = reader.GetDecimal(reader.GetOrdinal("amount"))
                });
            }
            return list;
        }

        public void AddDeduction(Deduction d)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"INSERT INTO deductions (deduction_name, deduction_type, amount)
                              VALUES (@name, @type, @amount)";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@name", d.DeductionName);
            cmd.Parameters.AddWithValue("@type", d.DeductionType);
            cmd.Parameters.AddWithValue("@amount", d.Amount);
            cmd.ExecuteNonQuery();
        }

        public void DeleteDeduction(int id)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            string query = "DELETE FROM deductions WHERE deduction_id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void AssignDeduction(int employeeId, int deductionId, decimal? customAmount)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"INSERT INTO employee_deductions (employee_id, deduction_id, custom_amount, is_active)
                              VALUES (@empId, @dedId, @custom, 1)";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@empId", employeeId);
            cmd.Parameters.AddWithValue("@dedId", deductionId);
            cmd.Parameters.AddWithValue("@custom", (object?)customAmount ?? DBNull.Value);
            cmd.ExecuteNonQuery();

            int newId = (int)cmd.LastInsertedId;
            LogHistory(newId, "Assigned", customAmount);
        }

        public void EditAssignment(int id, decimal? customAmount)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            string query = "UPDATE employee_deductions SET custom_amount = @custom WHERE id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@custom", (object?)customAmount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            LogHistory(id, "Edited", customAmount);
        }

        public void DeactivateAssignment(int id)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            string query = "UPDATE employee_deductions SET is_active = 0 WHERE id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            LogHistory(id, "Deactivated", null);
        }

        public void ReactivateAssignment(int id)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            string query = "UPDATE employee_deductions SET is_active = 1 WHERE id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            LogHistory(id, "Reactivated", null);
        }

        private void LogHistory(int employeeDeductionId, string action, decimal? customAmount)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            string query = @"INSERT INTO deduction_assignment_history (employee_deduction_id, action, custom_amount, changed_at)
                              VALUES (@edId, @action, @amount, NOW())";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@edId", employeeDeductionId);
            cmd.Parameters.AddWithValue("@action", action);
            cmd.Parameters.AddWithValue("@amount", (object?)customAmount ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public List<DeductionAssignmentHistory> GetAssignmentHistory(int employeeDeductionId)
        {
            var list = new List<DeductionAssignmentHistory>();
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"SELECT * FROM deduction_assignment_history
                              WHERE employee_deduction_id = @id
                              ORDER BY changed_at DESC";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", employeeDeductionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int amountOrdinal = reader.GetOrdinal("custom_amount");
                list.Add(new DeductionAssignmentHistory
                {
                    HistoryId = reader.GetInt32(reader.GetOrdinal("history_id")),
                    EmployeeDeductionId = reader.GetInt32(reader.GetOrdinal("employee_deduction_id")),
                    Action = reader.GetString(reader.GetOrdinal("action")),
                    CustomAmount = reader.IsDBNull(amountOrdinal) ? null : reader.GetDecimal(amountOrdinal),
                    ChangedAt = reader.GetDateTime(reader.GetOrdinal("changed_at"))
                });
            }
            return list;
        }

        public EmployeeDeduction? GetAssignmentById(int id)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"
                SELECT ed.id, ed.employee_id, ed.deduction_id, ed.custom_amount, ed.is_active,
                       e.first_name, e.last_name, d.deduction_name, d.deduction_type, d.amount
                FROM employee_deductions ed
                INNER JOIN employees e ON ed.employee_id = e.employee_id
                INNER JOIN deductions d ON ed.deduction_id = d.deduction_id
                WHERE ed.id = @id";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapAssignment(reader);
            }
            return null;
        }

        public List<EmployeeDeduction> GetAllAssignments()
        {
            var list = new List<EmployeeDeduction>();
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"
                SELECT ed.id, ed.employee_id, ed.deduction_id, ed.custom_amount, ed.is_active,
                       e.first_name, e.last_name, d.deduction_name, d.deduction_type, d.amount
                FROM employee_deductions ed
                INNER JOIN employees e ON ed.employee_id = e.employee_id
                INNER JOIN deductions d ON ed.deduction_id = d.deduction_id
                ORDER BY e.last_name";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapAssignment(reader));
            }
            return list;
        }

        public List<EmployeeDeduction> ComputeDeductionsForEmployee(int employeeId, decimal basicSalary)
        {
            var list = new List<EmployeeDeduction>();
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"
                SELECT ed.id, ed.employee_id, ed.deduction_id, ed.custom_amount, ed.is_active,
                       e.first_name, e.last_name, d.deduction_name, d.deduction_type, d.amount
                FROM employee_deductions ed
                INNER JOIN employees e ON ed.employee_id = e.employee_id
                INNER JOIN deductions d ON ed.deduction_id = d.deduction_id
                WHERE ed.employee_id = @empId AND ed.is_active = 1";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@empId", employeeId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var item = MapAssignment(reader);
                item.ComputedAmount = item.CustomAmount.HasValue
                    ? item.CustomAmount.Value
                    : (item.DeductionType == "Percentage" ? basicSalary * (item.Amount / 100m) : item.Amount);
                list.Add(item);
            }
            return list;
        }

        public decimal GetTotalDeductions(int employeeId, decimal basicSalary)
        {
            return ComputeDeductionsForEmployee(employeeId, basicSalary).Sum(d => d.ComputedAmount);
        }

        private EmployeeDeduction MapAssignment(MySqlDataReader reader)
        {
            int customOrdinal = reader.GetOrdinal("custom_amount");
            return new EmployeeDeduction
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
                DeductionId = reader.GetInt32(reader.GetOrdinal("deduction_id")),
                CustomAmount = reader.IsDBNull(customOrdinal) ? null : reader.GetDecimal(customOrdinal),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                EmployeeName = reader.GetString(reader.GetOrdinal("first_name")) + " " +
                               reader.GetString(reader.GetOrdinal("last_name")),
                DeductionName = reader.GetString(reader.GetOrdinal("deduction_name")),
                DeductionType = reader.GetString(reader.GetOrdinal("deduction_type")),
                Amount = reader.GetDecimal(reader.GetOrdinal("amount"))
            };
        }
    }
}
