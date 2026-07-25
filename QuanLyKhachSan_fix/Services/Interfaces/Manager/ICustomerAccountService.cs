using QuanLyKhachSan_fix.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Interfaces.Manager
{
    public interface ICustomerAccountService
    {
        Task<List<User>> GetAllCustomersAsync();
        Task<bool> ToggleActiveAsync(int userId);
        Task<List<Booking>> GetBookingHistoryAsync(int userId);
    }
}