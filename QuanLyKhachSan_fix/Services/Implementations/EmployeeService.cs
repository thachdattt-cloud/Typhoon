using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces;
using QuanLyKhachSan_fix.Services.Security;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Implementations
{
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _db;

        public EmployeeService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await _db.Employees
                .Include(e => e.User)
                .OrderBy(e => e.User.FullName)
                .ToListAsync();
        }

        public async Task<EmployeeResult> CreateEmployeeAsync(string username, string password, string fullName,
            string email, string phone, string position, decimal? salary)
        {
            bool usernameExists = await _db.Users.AnyAsync(u => u.Username == username);
            if (usernameExists)
            {
                return new EmployeeResult { Success = false, ErrorMessage = "Tên đăng nhập đã được sử dụng." };
            }

            var user = new User
            {
                Username = username,
                PasswordHash = PasswordHasher.HashPassword(password),
                FullName = fullName,
                Email = email,
                Phone = phone,
                Role = position, // "receptionist" hoac "manager"
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(); // luu truoc de co user.Id

            var employee = new Employee
            {
                UserId = user.Id,
                Position = position,
                HireDate = DateTime.Now,
                Salary = salary
            };

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();

            employee.User = user;
            return new EmployeeResult { Success = true, Employee = employee };
        }

        public async Task<bool> UpdateEmployeeAsync(int employeeId, string fullName, string phone, string position, decimal? salary)
        {
            var employee = await _db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee == null) return false;

            employee.User.FullName = fullName;
            employee.User.Phone = phone;
            employee.Position = position;
            employee.User.Role = position;
            employee.Salary = salary;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateEmployeeAsync(int employeeId)
        {
            var employee = await _db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee == null) return false;

            employee.User.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}