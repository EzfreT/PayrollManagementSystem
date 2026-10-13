using System.Text.RegularExpressions;
using MySqlConnector;
using PayrollManagementSystemMVC.Models;
using PayrollManagementSystemMVC.Models.Database;

namespace PayrollManagementSystemMVC.Services
{
    public class EmployeeService
    {
        public List<Employee> GetAllEmployees()
        {
            var list = new List<Employee>();
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = "SELECT * FROM employees ORDER BY last_name";
            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToEmployee(reader));
            }
            return list;
        }

        public Employee? GetEmployeeById(int id)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = "SELECT * FROM employees WHERE employee_id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapReaderToEmployee(reader);
            }
            return null;
        }

        public string GetNextEmployeeNumber()
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = "SELECT employee_number FROM employees";
            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            int max = 0;
            while (reader.Read())
            {
                string? num = reader.IsDBNull(0) ? null : reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(num))
                {
                    var match = Regex.Match(num, @"(\d+)$");
                    if (match.Success && int.TryParse(match.Value, out int val))
                    {
                        if (val > max) max = val;
                    }
                }
            }

            int next = max + 1;
            return $"EMP-{next:D4}";
        }

        private bool EmployeeNumberExists(string employeeNumber, int excludeId)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = "SELECT COUNT(*) FROM employees WHERE employee_number = @num AND employee_id != @excludeId";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@num", employeeNumber);
            cmd.Parameters.AddWithValue("@excludeId", excludeId);
            long count = (long)cmd.ExecuteScalar()!;
            return count > 0;
        }

        public string? ValidateEmployee(Employee emp)
        {
            if (string.IsNullOrWhiteSpace(emp.EmployeeNumber)) return "Employee Number is required.";
            if (string.IsNullOrWhiteSpace(emp.FirstName)) return "First Name is required.";
            if (string.IsNullOrWhiteSpace(emp.LastName)) return "Last Name is required.";
            if (string.IsNullOrWhiteSpace(emp.Gender)) return "Gender is required.";
            if (!emp.BirthDate.HasValue) return "Birth Date is required.";
            if (string.IsNullOrWhiteSpace(emp.Position)) return "Position is required.";
            if (string.IsNullOrWhiteSpace(emp.Department)) return "Department is required.";
            if (!emp.DateHired.HasValue) return "Date Hired is required.";
            if (string.IsNullOrWhiteSpace(emp.ContactNumber)) return "Contact Number is required.";
            if (string.IsNullOrWhiteSpace(emp.Address)) return "Address is required.";
            if (string.IsNullOrWhiteSpace(emp.Email)) return "Email is required.";

            if (EmployeeNumberExists(emp.EmployeeNumber, emp.EmployeeId))
                return $"Employee Number '{emp.EmployeeNumber}' is already in use.";

            bool validEmail = Regex.IsMatch(emp.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!validEmail) return "Email address is not valid.";

            bool validContact = Regex.IsMatch(emp.ContactNumber, @"^[0-9+\-\s]{7,15}$");
            if (!validContact) return "Contact number is not valid.";

            if (emp.BirthDate.Value.Date >= DateTime.Today) return "Birth date must be in the past.";
            if (emp.DateHired.Value.Date <= emp.BirthDate.Value.Date) return "Date Hired must be after the Birth Date.";

            return null;
        }

        public void AddEmployee(Employee emp)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"INSERT INTO employees
                (employee_number, first_name, middle_name, last_name, position, department,
                 gender, birth_date, date_hired, contact_number, address, email, status)
                VALUES
                (@empNum, @first, @middle, @last, @position, @department,
                 @gender, @birth, @hired, @contact, @address, @email, @status)";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@empNum", emp.EmployeeNumber);
            cmd.Parameters.AddWithValue("@first", emp.FirstName);
            cmd.Parameters.AddWithValue("@middle", (object?)emp.MiddleName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@last", emp.LastName);
            cmd.Parameters.AddWithValue("@position", (object?)emp.Position ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@department", (object?)emp.Department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@gender", (object?)emp.Gender ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@birth", (object?)emp.BirthDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hired", (object?)emp.DateHired ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@contact", (object?)emp.ContactNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@address", (object?)emp.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)emp.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", emp.Status);
            cmd.ExecuteNonQuery();
        }

        public void UpdateEmployee(Employee emp)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"UPDATE employees SET
                employee_number = @empNum, first_name = @first, middle_name = @middle, last_name = @last,
                position = @position, department = @department, gender = @gender, birth_date = @birth,
                date_hired = @hired, contact_number = @contact, address = @address, email = @email, status = @status
                WHERE employee_id = @id";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@empNum", emp.EmployeeNumber);
            cmd.Parameters.AddWithValue("@first", emp.FirstName);
            cmd.Parameters.AddWithValue("@middle", (object?)emp.MiddleName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@last", emp.LastName);
            cmd.Parameters.AddWithValue("@position", (object?)emp.Position ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@department", (object?)emp.Department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@gender", (object?)emp.Gender ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@birth", (object?)emp.BirthDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hired", (object?)emp.DateHired ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@contact", (object?)emp.ContactNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@address", (object?)emp.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)emp.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", emp.Status);
            cmd.Parameters.AddWithValue("@id", emp.EmployeeId);
            cmd.ExecuteNonQuery();
        }

        public void DeactivateEmployee(int id)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            string query = "UPDATE employees SET status = 'Inactive' WHERE employee_id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void ReactivateEmployee(int id)
        {
            using var conn = DbConnection.GetConnection();
            conn.Open();
            string query = "UPDATE employees SET status = 'Active' WHERE employee_id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public List<Employee> SearchEmployees(string keyword)
        {
            var list = new List<Employee>();
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"SELECT * FROM employees
                WHERE first_name LIKE @kw OR last_name LIKE @kw OR employee_number LIKE @kw
                ORDER BY last_name";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToEmployee(reader));
            }
            return list;
        }

        private Employee MapReaderToEmployee(MySqlDataReader reader)
        {
            int middleOrdinal = reader.GetOrdinal("middle_name");
            int positionOrdinal = reader.GetOrdinal("position");
            int departmentOrdinal = reader.GetOrdinal("department");
            int genderOrdinal = reader.GetOrdinal("gender");
            int birthOrdinal = reader.GetOrdinal("birth_date");
            int hiredOrdinal = reader.GetOrdinal("date_hired");
            int contactOrdinal = reader.GetOrdinal("contact_number");
            int addressOrdinal = reader.GetOrdinal("address");
            int emailOrdinal = reader.GetOrdinal("email");

            return new Employee
            {
                EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
                EmployeeNumber = reader.IsDBNull(reader.GetOrdinal("employee_number")) ? "" : reader.GetString(reader.GetOrdinal("employee_number")),
                FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                MiddleName = reader.IsDBNull(middleOrdinal) ? null : reader.GetString(middleOrdinal),
                LastName = reader.GetString(reader.GetOrdinal("last_name")),
                Position = reader.IsDBNull(positionOrdinal) ? null : reader.GetString(positionOrdinal),
                Department = reader.IsDBNull(departmentOrdinal) ? null : reader.GetString(departmentOrdinal),
                Gender = reader.IsDBNull(genderOrdinal) ? null : reader.GetString(genderOrdinal),
                BirthDate = reader.IsDBNull(birthOrdinal) ? null : reader.GetDateTime(birthOrdinal),
                DateHired = reader.IsDBNull(hiredOrdinal) ? null : reader.GetDateTime(hiredOrdinal),
                ContactNumber = reader.IsDBNull(contactOrdinal) ? null : reader.GetString(contactOrdinal),
                Address = reader.IsDBNull(addressOrdinal) ? null : reader.GetString(addressOrdinal),
                Email = reader.IsDBNull(emailOrdinal) ? null : reader.GetString(emailOrdinal),
                Status = reader.GetString(reader.GetOrdinal("status"))
            };
        }
    }
}
