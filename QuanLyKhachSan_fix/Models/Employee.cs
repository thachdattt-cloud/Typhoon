using QuanLyKhachSan_fix.Models;
using System;

namespace QuanLyKhachSan_fix.Models
{
    // position: receptionist | manager
    public class Employee
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Position { get; set; }
        public DateTime? HireDate { get; set; }
        public decimal? Salary { get; set; }

        public User User { get; set; } = null!;
    }
}