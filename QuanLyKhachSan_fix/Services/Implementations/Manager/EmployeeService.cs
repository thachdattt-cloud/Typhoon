using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Security;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuanLyKhachSan_fix.Services.Interfaces.Manager;

namespace QuanLyKhachSan_fix.Services.Implementations.Manager
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public EmployeeService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Employees
                .Include(e => e.User)
                .OrderBy(e => e.User.FullName)
                .ToListAsync();
        }

        public async Task<EmployeeResult> CreateEmployeeAsync(string username, string password, string fullName,
            string email, string phone, string position, decimal? salary)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            bool usernameExists = await db.Users.AnyAsync(u => u.Username == username);
            if (usernameExists)
            {
                return new EmployeeResult { Success = false, ErrorMessage = "Tên đăng nhập đã được sử dụng." };
            }
            bool emailExists = await db.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                return new EmployeeResult { Success = false, ErrorMessage = "Email này đã được sử dụng." };
            }

            var user = new User
            {
                Username = username,
                PasswordHash = PasswordHasher.HashPassword(password),
                FullName = fullName,
                Email = email,
                Phone = phone,
                Role = position,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var employee = new Employee
            {
                UserId = user.Id,
                Position = position,
                HireDate = DateTime.Now,
                Salary = salary
            };

            db.Employees.Add(employee);
            await db.SaveChangesAsync();

            employee.User = user;
            return new EmployeeResult { Success = true, Employee = employee };
        }

        public async Task<bool> UpdateEmployeeAsync(int employeeId, string fullName, string phone, string position, decimal? salary)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var employee = await db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee == null) return false;

            employee.User.FullName = fullName;
            employee.User.Phone = phone;
            employee.Position = position;
            employee.User.Role = position;
            employee.Salary = salary;

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateEmployeeAsync(int employeeId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var employee = await db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee == null) return false;

            employee.User.IsActive = false;
            await db.SaveChangesAsync();
            return true;
        }

    }
}