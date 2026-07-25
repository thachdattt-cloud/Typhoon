using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Interfaces.Manager
{
    public class EmployeeResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Employee? Employee { get; set; }
    }

    public interface IEmployeeService
    {
        Task<List<Employee>> GetAllEmployeesAsync();

        // Tao ca User (role = receptionist/manager) + Employee cung luc
        Task<EmployeeResult> CreateEmployeeAsync(string username, string password, string fullName,
            string email, string phone, string position, decimal? salary);

        Task<bool> UpdateEmployeeAsync(int employeeId, string fullName, string phone, string position, decimal? salary);

        Task<bool> DeactivateEmployeeAsync(int employeeId);
    }
}