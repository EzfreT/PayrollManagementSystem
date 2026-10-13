using MySqlConnector;

namespace PayrollManagementSystemMVC.Models.Database
{
    public class DbConnection
    {
        // Reads from environment variables when they're set (used on Railway / any live host).
        // Falls back to your local MySQL setup when they're not set (your own machine).
        private static string Server => Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
        private static string Database => Environment.GetEnvironmentVariable("DB_NAME") ?? "payroll_db";
        private static string User => Environment.GetEnvironmentVariable("DB_USER") ?? "root";
        private static string Password => Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "admin123";
        private static string Port => Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";

        public static MySqlConnection GetConnection()
        {
            string connString = $"Server={Server};Port={Port};Database={Database};User={User};Password={Password};";
            return new MySqlConnection(connString);
        }
    }
}