using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyKhachSan_fix.Models;

namespace QuanLyKhachSan_fix.Services.Interfaces.Customer
{
    public class BookingResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Booking? Booking { get; set; }
    }

    public interface IBookingService
    {
        // Tao dat phong moi cho 1 khach hang, 1 phong, trong 1 khoang ngay
        Task<BookingResult> CreateBookingAsync(int customerId, int roomId, DateTime checkInDate, DateTime checkOutDate, int guestCount);

        // Danh sach dat phong cua 1 khach hang (dung cho FormMyBookings)
        Task<List<Booking>> GetBookingsByCustomerAsync(int customerId);

        // Chi tiet 1 dat phong (dung khi mo FormPayment / FormEditRequest)
        Task<Booking?> GetBookingByIdAsync(int bookingId);

        // Khach tu huy dat phong cua chinh minh (chi khi con o trang thai pending/confirmed)
        Task<BookingResult> CancelBookingAsync(int bookingId, int customerId, string? reason);
    }
}
