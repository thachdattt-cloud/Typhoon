using QuanLyKhachSan_fix.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Interfaces
{
    public class CheckInOutResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface ICheckInOutService
    {
        Task<List<BookingDetail>> GetTodayArrivalsAsync();
        Task<List<BookingDetail>> GetTodayDeparturesAsync();
        Task<List<BookingDetail>> SearchReservedByBookingCodeAsync(string bookingCode);
        Task<List<BookingDetail>> SearchCheckedInByBookingCodeAsync(string bookingCode);
        Task<CheckInOutResult> CheckInAsync(int bookingDetailId, int staffId);
        Task<CheckInOutResult> CheckOutAsync(int bookingDetailId, int staffId);
    }
}
