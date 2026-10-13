using MySqlConnector;
using PayrollManagementSystemMVC.Models;
using PayrollManagementSystemMVC.Models.Database;

namespace PayrollManagementSystemMVC.Services
{
    public class AttendanceService
    {
        private static readonly TimeSpan StandardTimeIn = new TimeSpan(8, 0, 0);
        private static readonly TimeSpan StandardTimeOut = new TimeSpan(17, 0, 0);
        private const decimal StandardWorkHours = 8m; // after subtracting 1-hour break
        private const decimal BreakHours = 1m;

        public void RecordAttendance(Attendance att)
        {
            if (att.IsAbsent)
            {
                att.TimeIn = null;
                att.TimeOut = null;
                att.HoursWorked = 0;
                att.Status = "Absent";
            }
            else if (att.TimeIn.HasValue && att.TimeOut.HasValue)
            {
                decimal rawSpanHours = (decimal)(att.TimeOut.Value - att.TimeIn.Value).TotalHours;

                // Subtract the standard 1-hour lunch break, per the brief's example
                decimal netHours = rawSpanHours > BreakHours ? rawSpanHours - BreakHours : 0;
                att.HoursWorked = netHours;

                CalculateHourBreakdown(att);
            }

            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"INSERT INTO attendance
                (employee_id, attendance_date, time_in, time_out, hours_worked, status)
                VALUES (@empId, @date, @timeIn, @timeOut, @hours, @status)";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@empId", att.EmployeeId);
            cmd.Parameters.AddWithValue("@date", att.AttendanceDate);
            cmd.Parameters.AddWithValue("@timeIn", att.TimeIn ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@timeOut", att.TimeOut ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@hours", att.HoursWorked ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@status", att.Status);
            cmd.ExecuteNonQuery();
        }

        // Fills in LateHours, UndertimeHours, OvertimeHours, and sets Status accordingly
        private void CalculateHourBreakdown(Attendance att)
        {
            var timeIn = att.TimeIn!.Value;
            var timeOut = att.TimeOut!.Value;

            att.LateHours = timeIn > StandardTimeIn
                ? (decimal)(timeIn - StandardTimeIn).TotalHours
                : 0;

            att.UndertimeHours = timeOut < StandardTimeOut
                ? (decimal)(StandardTimeOut - timeOut).TotalHours
                : 0;

            att.OvertimeHours = (att.HoursWorked ?? 0) > StandardWorkHours
                ? (att.HoursWorked ?? 0) - StandardWorkHours
                : 0;

            if (att.LateHours > 0) att.Status = "Late";
            else if (att.UndertimeHours > 0) att.Status = "Undertime";
            else if (att.OvertimeHours > 0) att.Status = "Overtime";
            else att.Status = "Present";
        }

        public List<Attendance> GetAllAttendance()
        {
            var list = new List<Attendance>();
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"
                SELECT a.attendance_id, a.employee_id, a.attendance_date, a.time_in,
                       a.time_out, a.hours_worked, a.status, e.first_name, e.last_name
                FROM attendance a
                INNER JOIN employees e ON a.employee_id = e.employee_id
                ORDER BY a.attendance_date DESC";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToAttendance(reader));
            }
            return list;
        }

        public List<Attendance> GetAttendanceByEmployee(int employeeId)
        {
            var list = new List<Attendance>();
            using var conn = DbConnection.GetConnection();
            conn.Open();

            string query = @"
                SELECT a.attendance_id, a.employee_id, a.attendance_date, a.time_in,
                       a.time_out, a.hours_worked, a.status, e.first_name, e.last_name
                FROM attendance a
                INNER JOIN employees e ON a.employee_id = e.employee_id
                WHERE a.employee_id = @empId
                ORDER BY a.attendance_date DESC";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@empId", employeeId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToAttendance(reader));
            }
            return list;
        }

        private Attendance MapReaderToAttendance(MySqlDataReader reader)
        {
            int timeInOrdinal = reader.GetOrdinal("time_in");
            int timeOutOrdinal = reader.GetOrdinal("time_out");
            int hoursOrdinal = reader.GetOrdinal("hours_worked");

            var att = new Attendance
            {
                AttendanceId = reader.GetInt32(reader.GetOrdinal("attendance_id")),
                EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
                AttendanceDate = reader.GetDateTime(reader.GetOrdinal("attendance_date")),
                TimeIn = reader.IsDBNull(timeInOrdinal) ? null : reader.GetTimeSpan(timeInOrdinal),
                TimeOut = reader.IsDBNull(timeOutOrdinal) ? null : reader.GetTimeSpan(timeOutOrdinal),
                HoursWorked = reader.IsDBNull(hoursOrdinal) ? null : reader.GetDecimal(hoursOrdinal),
                Status = reader.GetString(reader.GetOrdinal("status")),
                EmployeeName = reader.GetString(reader.GetOrdinal("first_name")) + " " +
                               reader.GetString(reader.GetOrdinal("last_name"))
            };

            att.IsAbsent = att.Status == "Absent";

            // Recompute the hour breakdown for display, since we don't store these 3 values
            if (att.TimeIn.HasValue && att.TimeOut.HasValue)
            {
                CalculateHourBreakdown(att);
            }

            return att;
        }
    }
}
