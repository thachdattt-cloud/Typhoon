using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuanLyKhachSan_fix.Services.Interfaces.Manager;

namespace QuanLyKhachSan_fix.Services.Implementations.Manager
{
    public class CustomerAccountService : ICustomerAccountService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public CustomerAccountService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<User>> GetAllCustomersAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Users
                .Where(u => u.Role == "customer")
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<bool> ToggleActiveAsync(int userId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.Users.FindAsync(userId);
            if (user == null || user.Role != "customer") return false;

            user.IsActive = !user.IsActive;
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<List<Booking>> GetBookingHistoryAsync(int userId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Bookings
                .Where(b => b.CustomerId == userId)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Room)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }
    }
}