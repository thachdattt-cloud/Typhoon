using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Implementations
{
    public class CustomerAccountService : ICustomerAccountService
    {
        private readonly AppDbContext _db;

        public CustomerAccountService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<User>> GetAllCustomersAsync()
        {
            return await _db.Users
                .Where(u => u.Role == "customer")
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<bool> ToggleActiveAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null || user.Role != "customer") return false;

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<Booking>> GetBookingHistoryAsync(int userId)
        {
            return await _db.Bookings
                .Where(b => b.CustomerId == userId)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Room)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }
    }
}