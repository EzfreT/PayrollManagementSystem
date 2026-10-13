-- ============================================================
-- Payroll Management System - Full Database Rebuild Script
-- Run this entire script against a fresh MySQL 8.0 / MariaDB
-- server to recreate payroll_db exactly as it was before the
-- reformat. Run top to bottom in one go.
-- ============================================================

CREATE DATABASE IF NOT EXISTS payroll_db;
USE payroll_db;

-- ------------------------------------------------------------
-- 1. EMPLOYEES
-- ------------------------------------------------------------
CREATE TABLE employees (
    employee_id     INT AUTO_INCREMENT PRIMARY KEY,
    employee_number VARCHAR(20) NULL,
    first_name      VARCHAR(50) NOT NULL,
    middle_name     VARCHAR(50) NULL,
    last_name       VARCHAR(50) NOT NULL,
    position        VARCHAR(100) NULL,
    department      VARCHAR(100) NULL,
    gender          VARCHAR(10) NULL,
    birth_date      DATE NULL,
    date_hired      DATE NULL,
    contact_number  VARCHAR(20) NULL,
    address         VARCHAR(255) NULL,
    email           VARCHAR(100) NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'Active'
);

-- ------------------------------------------------------------
-- 2. SALARY
-- ------------------------------------------------------------
CREATE TABLE salary (
    salary_id      INT AUTO_INCREMENT PRIMARY KEY,
    employee_id    INT NOT NULL,
    basic_salary   DECIMAL(10,2) NOT NULL,
    pay_frequency  VARCHAR(20) NOT NULL DEFAULT 'Monthly',
    effective_date DATE NOT NULL,
    is_deleted     TINYINT(1) NOT NULL DEFAULT 0,
    FOREIGN KEY (employee_id) REFERENCES employees(employee_id)
);

-- ------------------------------------------------------------
-- 3. ATTENDANCE
-- ------------------------------------------------------------
CREATE TABLE attendance (
    attendance_id    INT AUTO_INCREMENT PRIMARY KEY,
    employee_id      INT NOT NULL,
    attendance_date  DATE NOT NULL,
    time_in          TIME NULL,
    time_out         TIME NULL,
    hours_worked     DECIMAL(5,2) NULL,
    status           VARCHAR(20) NOT NULL DEFAULT 'Present',
    FOREIGN KEY (employee_id) REFERENCES employees(employee_id)
);

-- ------------------------------------------------------------
-- 4. DEDUCTIONS (deduction types)
-- ------------------------------------------------------------
CREATE TABLE deductions (
    deduction_id    INT AUTO_INCREMENT PRIMARY KEY,
    deduction_name  VARCHAR(100) NOT NULL,
    deduction_type  VARCHAR(20) NOT NULL DEFAULT 'Fixed', -- 'Fixed' or 'Percentage'
    amount          DECIMAL(10,2) NOT NULL
);

-- ------------------------------------------------------------
-- 5. EMPLOYEE_DEDUCTIONS (assignments)
-- ------------------------------------------------------------
CREATE TABLE employee_deductions (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    employee_id     INT NOT NULL,
    deduction_id    INT NOT NULL,
    custom_amount   DECIMAL(10,2) NULL,
    is_active       TINYINT(1) NOT NULL DEFAULT 1,
    FOREIGN KEY (employee_id) REFERENCES employees(employee_id),
    FOREIGN KEY (deduction_id) REFERENCES deductions(deduction_id)
);

-- ------------------------------------------------------------
-- 6. DEDUCTION_ASSIGNMENT_HISTORY (audit log)
-- ------------------------------------------------------------
CREATE TABLE deduction_assignment_history (
    history_id              INT AUTO_INCREMENT PRIMARY KEY,
    employee_deduction_id   INT NOT NULL,
    action                  VARCHAR(20) NOT NULL, -- Assigned / Edited / Deactivated / Reactivated
    custom_amount           DECIMAL(10,2) NULL,
    changed_at              DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (employee_deduction_id) REFERENCES employee_deductions(id)
);

-- ------------------------------------------------------------
-- 7. PAYROLL (Week 5-6 — structure only, not yet built on)
-- ------------------------------------------------------------
CREATE TABLE payroll (
    payroll_id        INT AUTO_INCREMENT PRIMARY KEY,
    employee_id       INT NOT NULL,
    pay_period_start  DATE NOT NULL,
    pay_period_end    DATE NOT NULL,
    gross_pay         DECIMAL(10,2) NOT NULL DEFAULT 0,
    total_deductions  DECIMAL(10,2) NOT NULL DEFAULT 0,
    net_pay           DECIMAL(10,2) NOT NULL DEFAULT 0,
    status            VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending / Approved / Rejected
    created_at        DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (employee_id) REFERENCES employees(employee_id)
);

-- ------------------------------------------------------------
-- 8. PAYSLIP (Week 6 — structure only, not yet built on)
-- ------------------------------------------------------------
CREATE TABLE payslip (
    payslip_id    INT AUTO_INCREMENT PRIMARY KEY,
    payroll_id    INT NOT NULL,
    generated_at  DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (payroll_id) REFERENCES payroll(payroll_id)
);

-- ------------------------------------------------------------
-- 9. SEED DATA (matches what was on the old machine)
-- Comment these out or edit freely before running.
-- ------------------------------------------------------------

-- Example employee used for testing (Robert Pattinson, Manager, DFA)
-- INSERT INTO employees
--   (employee_number, first_name, middle_name, last_name, position, department,
--    gender, birth_date, date_hired, contact_number, address, email, status)
-- VALUES
--   ('EMP-0001', 'Robert', NULL, 'Pattinson', 'Manager', 'Department of Foreign Affairs',
--    'Male', '1986-05-13', '2026-09-20', '09171234567', '123 Sample St, Manila', 'robert.p@example.com', 'Active');

-- Example deduction types
-- INSERT INTO deductions (deduction_name, deduction_type, amount) VALUES ('SSS', 'Fixed', 200.00);
-- INSERT INTO deductions (deduction_name, deduction_type, amount) VALUES ('Tax', 'Percentage', 5.00);

-- ============================================================
-- End of script
-- ============================================================
